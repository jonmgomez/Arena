using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSpawnController : NetworkBehaviour
{
    private readonly Logger logger = new("SPAWN");

    public static PlayerSpawnController Instance { get; private set; }

    private List<Player> players = new();
    [SerializeField] private Player playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnDelay = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugSingleSpawn = false;

    private bool spawningEnabled = true;

    void Awake()
    {
        if (Instance != null)
        {
            logger.LogError("PlayerSpawnController already exists");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        InGameController.Instance.OnGameStart += () => SetSpawningEnabled(true);
        InGameController.Instance.OnGameRestart += RespawnAllPlayers;
        InGameController.Instance.OnGameEnd += () => SetSpawningEnabled(false);
    }

    public Player SpawnNewPlayerPrefab(ulong clientId)
    {
        logger.Log($"Spawning player for client {clientId}");
        Vector3 spawnPoint = GetSpawnPoint();
        spawnPoint.y += 0.5f;
        Player player = Instantiate(playerPrefab, spawnPoint, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        return player;
    }

    public void DestroyPlayer(Player player)
    {
        players.Remove(player);
        player.GetComponent<NetworkObject>().Despawn(true);
    }

    public void RespawnPlayer(Player player)
    {
        StartCoroutine(RespawnPlayerAfterDelay(player));
    }

    private void RespawnPlayerInternal(Player player)
    {
        if (!spawningEnabled) return;

        Vector3 spawnPoint = GetSpawnPoint();
        spawnPoint.y += player.GetComponent<CharacterController>().height / 2f;
        player.RespawnOnServer(spawnPoint);
        logger.Log($"Respawning player {PlayerToString(player)} at {spawnPoint}");
    }

    IEnumerator RespawnPlayerAfterDelay(Player player)
    {
        yield return new WaitForSeconds(respawnDelay);

        RespawnPlayerInternal(player);
    }

    public void RespawnAllPlayers()
    {
        players = GameState.Instance.GetPlayers();
        foreach (var player in players)
        {
            RespawnPlayerInternal(player);
        }
    }

    private Vector3 GetSpawnPoint()
    {
        if (debugSingleSpawn)
            return spawnPoints[0].position;
        else
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }

    private string PlayerToString(Player player)
    {
        return $"({player.GetName()}-{player.OwnerClientId})";
    }

    public void SetSpawningEnabled(bool enabled) => spawningEnabled = enabled;
}
