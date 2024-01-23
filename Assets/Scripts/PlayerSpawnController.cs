using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnController : NetworkBehaviour
{
    public static PlayerSpawnController Instance { get; private set; }

    List<Player> players = new List<Player>();
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] float respawnDelay = 1f;

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

    public void RegisterPlayer(Player player)
    {
        players.Add(player);
    }

    public void UnregisterPlayer(Player player)
    {
        players.Remove(player);
    }

    public void RespawnPlayer(Player player)
    {
        StartCoroutine(RespawnPlayerAfterDelay(player));
    }

    IEnumerator RespawnPlayerAfterDelay(Player player)
    {
        yield return new WaitForSeconds(respawnDelay);
        Debug.Log($"Respawning player {player.OwnerClientId}");
        player.RespawnOnServer(GetSpawnPoint(), player.OwnerClientId);
    }

    private Vector3 GetSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }
}
