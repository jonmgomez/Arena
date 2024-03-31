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

    public static InGameController Instance { get; private set; }

    [Tooltip("Time in seconds to wait before the last player that damaged another player is forgotten")]
    [SerializeField] private float timeoutLastPlayerDamaged = 5f;

    private readonly Dictionary<ulong, PlayerDamagedState> players = new();
    private EliminationFeed eliminationFeed;
    private ScoreBoard scoreBoard;
    private PlayerMaterialController playerMaterialController;

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
            Logger.Default.Log($"Player {key} last damaged timeout expired");
            players.Remove(key);
        }
    }

    public void PlayerSpawned(Player player)
    {
        scoreBoard.CreatePlayerScoreCard(player);
        player.GetPlayerScore().SyncScore();
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
