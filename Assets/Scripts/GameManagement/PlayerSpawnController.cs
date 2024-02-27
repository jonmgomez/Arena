using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSpawnController : NetworkBehaviour
{
    private Logger logger = new("SPAWN");

    public static PlayerSpawnController Instance { get; private set; }

    private readonly List<Player> players = new();
    [SerializeField] private Player playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnDelay = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugSingleSpawn = false;

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

    public Player SpawnNewPlayerPrefab(ulong clientId)
    {
        logger.Log($"Spawning player for client {clientId}");
        Player player = Instantiate(playerPrefab, GetSpawnPoint(), Quaternion.identity);
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

    IEnumerator RespawnPlayerAfterDelay(Player player)
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPoint = GetSpawnPoint();
        spawnPoint.y += player.GetComponent<CharacterController>().height / 4f;
        player.RespawnOnServer(spawnPoint);
        logger.Log($"Respawning player {PlayerToString(player)} at {spawnPoint}");
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
}
