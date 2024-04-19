using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InGameController : NetworkBehaviour
{
    private class PlayerDamagedState
    {
        public Player player;
        public ulong lastClientId;
        public float lastDamagedTimeOut = 0f;
    }

    private readonly Logger logger = new("GAME");
    public static InGameController Instance { get; private set; }

    [Tooltip("Time in seconds to wait before the last player that damaged another player is forgotten")]
    [SerializeField] private float timeoutLastPlayerDamaged = 5f;
    [SerializeField] private float gameStartTimer = 10;

    [Header("Debug")]
    [SerializeField] private bool useCountDownTimer = true;

    public event System.Action OnGameRestart;
    public event System.Action OnGameStart;

    private bool gameStarted = false;
    private List<Player> acknowledgedPlayers = new();
    private readonly Dictionary<ulong, PlayerDamagedState> players = new();
    private EliminationFeed eliminationFeed;
    private ScoreBoard scoreBoard;
    private PlayerMaterialController playerMaterialController;
    private WeaponSpawner[] weaponSpawners;
    private GameModeController gameModeController;

    public override void OnNetworkSpawn()
    {
        if (useCountDownTimer && IsServer)
        {
            StartGameCountdown();
        }
        else if (Net.IsClientOnly)
        {
            RequestGameCountDownTimerServerRpc(Net.LocalClientId);
        }
        else if (IsServer)
        {
            gameModeController.StartGame();
            OnGameStart?.Invoke();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        eliminationFeed = FindObjectOfType<EliminationFeed>(true);
        scoreBoard = FindObjectOfType<ScoreBoard>(true);
        playerMaterialController = GetComponent<PlayerMaterialController>();
        weaponSpawners = FindObjectsOfType<WeaponSpawner>();

        gameModeController = gameObject.AddComponent<FreeForAllController>();
    }

    private void Update()
    {
        List<ulong> toRemove = new();
        foreach (var player in players)
        {
            player.Value.lastDamagedTimeOut -= Time.deltaTime;
            if (player.Value.lastDamagedTimeOut <= 0f)
            {
                toRemove.Add(player.Key);
            }
        }

        foreach (var key in toRemove)
        {
            logger.Log($"Player {key} last damaged timeout expired");
            players.Remove(key);
        }
    }

    public void PlayerSpawned(Player player)
    {
        if (!gameStarted)
            GameSetup();

        if (!acknowledgedPlayers.Contains(player))
        {
            GetPlayerMaterial(player);
            scoreBoard.CreatePlayerScoreCard(player);
            player.GetPlayerScore().SyncScore();

            acknowledgedPlayers.Add(player);
        }
    }

    public void PlayerDespawned(ulong clientId)
    {
        scoreBoard.RemovePlayerScoreCard(clientId);

        List<ulong> toRemove = new();
        foreach (var player in players)
        {
            if (player.Value.lastClientId == clientId)
            {
                toRemove.Add(player.Key);
                break;
            }
        }

        foreach (ulong id in toRemove)
        {
            players.Remove(id);
        }

        acknowledgedPlayers.RemoveAll(p => p == null);
    }

    public void GameSetup()
    {
        if (!IsServer) return;

        gameStarted = true;

        foreach (var spawner in weaponSpawners)
        {
            spawner.SpawnWeaponOnStart();
        }
    }

    public void PlayerDamaged(Player player, ulong clientId, bool isAnonymous)
    {
        if (isAnonymous) return;

        if (players.ContainsKey(player.OwnerClientId))
        {
            PlayerDamagedState state = players[player.OwnerClientId];
            state.lastClientId = clientId;
            state.lastDamagedTimeOut = timeoutLastPlayerDamaged;
        }
        else
        {
            players.Add(player.OwnerClientId, new PlayerDamagedState
            {
                player = player,
                lastClientId = clientId,
                lastDamagedTimeOut = timeoutLastPlayerDamaged
            });
        }
    }

    public void PlayerDied(Player player, ulong clientId, bool isAnonymous)
    {
        bool lastDamagedPlayerFound = players.ContainsKey(player.OwnerClientId);
        Player eliminator = null;

        if (lastDamagedPlayerFound && isAnonymous)
        {
            eliminator = GameState.Instance.GetPlayer(players[player.OwnerClientId].lastClientId);
        }
        else if (!isAnonymous)
        {
            eliminator = GameState.Instance.GetPlayer(clientId);
        }

        string eliminatorName = eliminator != null ? eliminator.GetName() : null;
        eliminationFeed.AddEliminationEntry(eliminatorName, player.GetName());

        if (lastDamagedPlayerFound)
        {
            players.Remove(player.OwnerClientId);

            eliminator.GetPlayerScore().IncreaseScore(ScoreType.Elimination);
        }

        player.GetPlayerScore().IncreaseScore(ScoreType.Death);

        gameModeController.CheckGameScores();
    }

    public void PlayerHit(Player player, ulong clientId, float damage, bool headShot)
    {
        Player playerWhoShot = GameState.Instance.GetPlayer(clientId);
        if (playerWhoShot == null)
        {
            logger.LogError($"Player {clientId} not found");
            return;
        }
    }

    public void StartGameCountdown()
    {
        StartCoroutine(StartGameCountdownCoroutine());
    }

    private IEnumerator StartGameCountdownCoroutine()
    {
        FindObjectOfType<GameStartScreen>().ShowGameStartScreen(gameStartTimer);

        while (gameStartTimer > 0)
        {
            yield return null;
            gameStartTimer -= Time.deltaTime;
        }

        gameModeController.StartGame();
        OnGameStart?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGameCountDownTimerServerRpc(ulong clientId)
    {
        float timeLeft = useCountDownTimer ? gameStartTimer : 0;
        RequestGameCountDownTimerClientRpc(timeLeft, Utility.SendToOneClient(clientId));
    }

    [ClientRpc]
    private void RequestGameCountDownTimerClientRpc(float time, ClientRpcParams clientRpcParams = default)
    {
        gameStartTimer = time;
        StartGameCountdown();
    }

    public void OnGameEnded()
    {
        StartCoroutine(RestartGame());
    }

    private IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(10f);

        gameModeController.StartGame();
        OnGameStart?.Invoke();
        OnGameRestart?.Invoke();
    }

    public void GetPlayerMaterial(Player player)
    {
        playerMaterialController.GetMaterial(player.OwnerClientId);
    }

    public void UpdateScoreBoard()
    {
        scoreBoard.UpdateScoreBoard();
    }
}
