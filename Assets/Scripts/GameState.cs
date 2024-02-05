using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameState : NetworkBehaviour
{
    private class PlayerData
    {
        public ulong clientId;
        public Player player;
    }

    public static GameState Instance { get; private set; }

    List<PlayerData> connectedClients = new List<PlayerData>();

    // Is this is a in-game scene or a lobby/menu
    bool inGameScene = false;

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadComplete;
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton is null!");
        }
    }

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ValidateScene(SceneManager.GetActiveScene().name);
    }

    private void ValidateScene(string sceneName)
    {
        // Any scenes where a player must be spawned for clients are prefixed with "Arena"
        inGameScene = sceneName.StartsWith("Arena");
    }

    private void RegisterPlayer(ulong clientId)
    {
        connectedClients.Add(new PlayerData { clientId = clientId, player = null });
    }

    private void UnregisterPlayer(ulong clientId)
    {
        connectedClients.Remove(connectedClients.Find(p => p.clientId == clientId));
    }

    private PlayerData FindClient(ulong clientId)
    {
        return connectedClients.Find(p => p.clientId == clientId);
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client [{clientId}] " + (clientId == NetworkManager.Singleton.LocalClientId ? "(Self) " : "") + "connected");
        RegisterPlayer(clientId);

        if (IsServer)
        {
            if (inGameScene)
            {
                Player spawnedPlayer = PlayerSpawnController.Instance.SpawnNewPlayerPrefab(clientId);
                FindClient(clientId).player = spawnedPlayer;
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client [{clientId}] disconnected");

        UnregisterPlayer(clientId);
    }

    private void OnNetworkSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode,
                                            List<ulong> clientsCompleted,
                                            List<ulong> clientsTimedOut)
    {
        Debug.Log($"Networked load scene complete. {sceneName} loaded. {clientsCompleted.Count} clients connected. {clientsTimedOut.Count} clients timed out.");

        if (IsServer)
        {
            ValidateScene(sceneName);
            this.Invoke(() => {
                foreach (ulong client in clientsCompleted)
                {
                    if (inGameScene)
                    {
                        Player spawnedPlayer = PlayerSpawnController.Instance.SpawnNewPlayerPrefab(client);
                        FindClient(client).player = spawnedPlayer;
                    }
                }
            }, 1f);
        }
    }
}
