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
        Debug.Log($"[SERVER] Respawning player {player.OwnerClientId}");
        Vector3 spawnPoint = GetSpawnPoint();
        spawnPoint.y += player.GetComponent<CharacterController>().height / 2f;
        player.RespawnOnServer(spawnPoint);
    }

    private Vector3 GetSpawnPoint()
    {
        if (debugSingleSpawn)
            return spawnPoints[0].position;
        else
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }
}
