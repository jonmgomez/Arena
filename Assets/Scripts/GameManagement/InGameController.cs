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
    [Tooltip("Use a countdown timer before the game starts. This only uses the server's value")]
    [SerializeField] private bool useCountDownTimer = true;

    public event System.Action OnGameRestart;
    public event System.Action OnGameStart;
    public event System.Action OnGameEnd;

    private bool gameStarted = false;
    private readonly List<Player> acknowledgedPlayers = new();
    private readonly Dictionary<ulong, PlayerDamagedState> players = new();
    private EliminationFeed eliminationFeed;
    private ScoreBoard scoreBoard;
    private PlayerMaterialController playerMaterialController;
    private WeaponSpawner[] weaponSpawners;
    private GameModeController gameModeController;

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

        GameSetupData gameSetupData = FindObjectOfType<GameSetupData>();
        if (gameSetupData != null)
        {
            switch (gameSetupData.GameMode.Value)
            {
                case GameMode.FreeForAll:
                    gameModeController = GetComponent<FreeForAllGameMode>();

                    FreeForAllGameMode freeForAllGameMode = gameModeController as FreeForAllGameMode;
                    freeForAllGameMode.SetMaxTime(gameSetupData.TimeLimit.Value);
                    freeForAllGameMode.SetScoreLimit(gameSetupData.ScoreLimit.Value);

                    break;
                default:
                    logger.LogError($"Game mode {gameSetupData.GameMode} not found");
                    break;
            }
            Destroy(gameSetupData.gameObject);
        }
        else
        {
            gameModeController = GetComponent<FreeForAllGameMode>();
        }
    }

    private void Start()
    {
        scoreBoard.SetGameMode(gameModeController);
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
        // Due to where players can be spawned from server/client side.
        // This may be called multiple times with the same player,
        // so check if we have already acknowledged this player
        if (!acknowledgedPlayers.Contains(player))
        {
            GetPlayerMaterial(player);
            scoreBoard.CreatePlayerScoreCard(player);
            player.GetPlayerScore().SyncScore();

            acknowledgedPlayers.Add(player);

            // If this is the local player setup the game
            if (player == GameState.Instance.GetLocalPlayer())
            {
                if (!gameStarted)
                    GameSetup();

                player.SetEnableControls(false);

                if (IsServer)
                {
                    if (!useCountDownTimer)
                        gameStartTimer = 0;

                    StartGameCountdown();
                }
                else if (Net.IsClientOnly)
                {
                    RequestGameInformationServerRpc(Net.LocalClientId);
                }
            }
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

        gameModeController.CheckGameScoresForWin();
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

        StartGame();
    }

    /// <summary>
    /// When the client joins they request various information about the game.
    /// Server sends back the information requested to that client.
    /// ex. Game mode, time left, etc.
    /// </summary>
    /// <param name="clientId">Id of client requesting information</param>
    [ServerRpc(RequireOwnership = false)]
    private void RequestGameInformationServerRpc(ulong clientId)
    {
        float timeLeft = useCountDownTimer ? gameStartTimer : 0;
        RequestGameInformationClientRpc(timeLeft,
                                        gameModeController.GetMaxTime(), gameModeController.GetTimeLeft(),
                                        Utility.SendToOneClient(clientId));
    }

    /// <summary>
    /// Response from the server to the client with the requested game information.
    /// </summary>
    /// <param name="gameStartCountDownTimer">Pre game timer countdown</param>
    /// <param name="gameTimer">Time left for game mode</param>
    [ClientRpc]
    private void RequestGameInformationClientRpc(float gameStartCountDownTimer, float gameMaxTime, float gameTimer, ClientRpcParams clientRpcParams = default)
    {
        gameStartTimer = gameStartCountDownTimer;
        gameModeController.SetMaxTime(gameMaxTime);
        StartGameCountdown();

        // Must be done after since game may start before this is called
        gameModeController.SetTimeLeft(gameTimer);
    }

    public void OnGameEnded()
    {
        OnGameEnd?.Invoke();
        StartCoroutine(RestartGameTimer());
    }

    private IEnumerator RestartGameTimer()
    {
        yield return new WaitForSeconds(10f);

        RestartGame();
    }

    private void StartGame()
    {
        gameModeController.StartGame();
        OnGameStart?.Invoke();
    }

    private void RestartGame()
    {
        StartGame();
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
