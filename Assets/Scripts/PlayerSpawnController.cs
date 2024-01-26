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
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] float respawnDelay = 1f;

    [SerializeField] GameObject screenCover;

    [Header("Debug")]
    [SerializeField] bool debugSingleSpawn = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadComplete;
        }
    }

    private void OnNetworkSceneLoadComplete(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        this.Invoke(() => {
            foreach (ulong client in clientsCompleted)
            {
                Debug.Log($"[SERVER] Spawning player for client {client}");
                GameObject playerObject = Instantiate(playerPrefab, GetSpawnPoint(), Quaternion.identity);
                playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(client, true);
            }
            GameLoadedClientRpc();

            if (IsServer && !IsHost)
                screenCover.SetActive(false);
        }, 1f);
    }

    [ClientRpc]
    public void GameLoadedClientRpc()
    {
        screenCover.SetActive(false);
    }

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
