using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSpawnController : NetworkBehaviour
{
    public static PlayerSpawnController Instance { get; private set; }

    List<Player> players = new List<Player>();
    [SerializeField] Player playerPrefab;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] float respawnDelay = 1f;

    [Header("Debug")]
    [SerializeField] bool debugSingleSpawn = false;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("PlayerSpawnController already exists");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public Player SpawnNewPlayerPrefab(ulong clientId, string playerName = "")
    {
        Debug.Log($"[SERVER] Spawning player for client {clientId}");
        Player player = Instantiate(playerPrefab, GetSpawnPoint(), Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        if (playerName != "")
            player.SetName(playerName);

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
        spawnPoint.y += player.GetComponent<CharacterController>().height / 2f;
        player.RespawnOnServer(spawnPoint);
        Debug.Log($"[SERVER] Respawning player {player.OwnerClientId} at {spawnPoint}");
    }

    private Vector3 GetSpawnPoint()
    {
        if (debugSingleSpawn)
            return spawnPoints[0].position;
        else
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }
}
