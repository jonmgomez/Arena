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

    readonly Dictionary<ulong, PlayerDamagedState> players = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
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
            Logger.Log($"Player {key} last damaged timeout expired");
            players.Remove(key);
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
        if (players.ContainsKey(player.OwnerClientId) && isAnonymous)
        {
            Logger.Log($"Player {player.OwnerClientId} died, last damaged by {players[player.OwnerClientId].lastClientId}");
            players.Remove(player.OwnerClientId);
        }
        else if (!isAnonymous)
        {
            Logger.Log($"Player {player.OwnerClientId} died, last damaged by {clientId}");
        }
        else
        {
            Logger.Log($"Player {player.OwnerClientId} died from unknown source");
        }
    }
}
