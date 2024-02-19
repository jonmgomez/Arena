using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientData
{
    public ulong clientId;
    public string clientName;
    public Player player;
}

public class GameState : NetworkBehaviour
{
    public static GameState Instance { get; private set; }

    private readonly List<ClientData> connectedClients = new List<ClientData>();
    private ClientNamesSynchronizer clientNameSynchronizer;

    public event Action<ulong> OnNewClientConnected;

    float timeSinceGameStart = 0f;

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
            Logger.LogError("NetworkManager.Singleton is null!");
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        clientNameSynchronizer = GetComponent<ClientNamesSynchronizer>();
    }

    void Start()
    {
        ValidateScene(SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        if (inGameScene)
            timeSinceGameStart += Time.deltaTime;
    }

    private void ValidateScene(string sceneName)
    {
        // Any scenes where a player must be spawned for clients are prefixed with "Arena"
        inGameScene = sceneName.StartsWith("Arena");
    }

    public void RegisterPlayer(ulong clientId)
    {
        connectedClients.Add(new ClientData { clientId = clientId, player = null });
    }

    private void UnregisterPlayer(ulong clientId)
    {
        connectedClients.Remove(connectedClients.Find(clientData => clientData.clientId == clientId));
    }

    private ClientData FindClient(ulong clientId)
    {
        return connectedClients.Find(clientData => clientData.clientId == clientId);
    }

    #region NetworkEventCallbacks
    private void OnClientConnected(ulong clientId)
    {
        bool selfConnected = clientId == NetworkManager.Singleton.LocalClientId;
        Logger.Log($"Client [{clientId}] " + (selfConnected ? "(Self) " : "") + "connected");

        RegisterPlayer(clientId);

        OnNewClientConnected?.Invoke(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Logger.Log($"Client [{clientId}] disconnected");
        UnregisterPlayer(clientId);

        if (IsServer)
        {
            ClientDisconnectedClientRpc(clientId);
        }
    }

    [ClientRpc]
    public void ClientDisconnectedClientRpc(ulong clientId)
    {
        Logger.Log($"Client [{clientId}] disconnected");
        UnregisterPlayer(clientId);
    }

    private void OnNetworkSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode,
                                            List<ulong> clientsCompleted,
                                            List<ulong> clientsTimedOut)
    {
        Logger.Log($"Networked load scene complete. {sceneName} loaded. {clientsCompleted.Count} clients connected. {clientsTimedOut.Count} clients timed out.");

        if (IsServer)
        {
            ValidateScene(sceneName);

            this.Invoke(() => {
                foreach (ulong client in clientsCompleted)
                {
                    if (inGameScene)
                    {
                        ClientData clientData = FindClient(client);
                        Player spawnedPlayer = PlayerSpawnController.Instance.SpawnNewPlayerPrefab(client, clientData.clientName);
                        clientData.player = spawnedPlayer;
                    }
                }
            }, 1f);
        }
    }
    #endregion

    public void ClientReady(ulong clientId)
    {
        if (IsServer)
        {
            if (inGameScene)
            {
                ClientData client = FindClient(clientId);
                if (client != null)
                {
                    Player spawnedPlayer = PlayerSpawnController.Instance.SpawnNewPlayerPrefab(clientId, client.clientName);
                    client.player = spawnedPlayer;
                }
            }
        }
    }

    public void SetLocalClientName(string name)
    {
        clientNameSynchronizer.SetLocalClientName(name);
    }

    #region Setters/Getters
    public List<Player> GetActivePlayers()
    {
        List<Player> players = new();
        foreach (ClientData client in connectedClients)
        {
            if (client.player != null)
                players.Add(client.player);
        }

        return players;
    }

    public List<ClientData> GetConnectedClients()
    {
        return connectedClients;
    }

    public ClientData GetClientData(ulong clientId)
    {
        return FindClient(clientId);
    }

    public Player GetPlayer(ulong clientId)
    {
        ClientData client = FindClient(clientId);
        if (client != null)
            return client.player;

        return null;
    }
    #endregion
}
