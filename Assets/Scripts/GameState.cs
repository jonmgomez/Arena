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

    private readonly List<ClientData> connectedClients = new();
    private ClientNetwork clientNetwork;
    private ClientNamesSynchronizer clientNameSynchronizer;

    public event Action AllClientsReady;
    public event Action WaitingForClients;

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

        clientNetwork = GetComponent<ClientNetwork>();
        clientNetwork.OnClientsReady += ClientNetworkSynced;
        clientNetwork.OnWaitingForClients += () => WaitingForClients?.Invoke();
        clientNameSynchronizer = GetComponent<ClientNamesSynchronizer>();
        clientNameSynchronizer.OnClientsReady += ClientNamesSynced;
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

    private ClientData FindClient(ulong clientId)
    {
        return connectedClients.Find(clientData => clientData.clientId == clientId);
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

    private void ClientNetworkSynced()
    {
        CheckClientsReady();
    }

    private void ClientNamesSynced()
    {
        CheckClientsReady();
    }

    private void CheckClientsReady()
    {
        if (IsServer)
        {
            if (clientNetwork.AreClientsSynced() && clientNameSynchronizer.AreClientsSynced())
            {
                Logger.Log("All clients are ready. Game is able to start");
                AllClientsReady?.Invoke();
            }
        }
    }

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

    #region Setters/Getters
    public void SetLocalClientName(string name)
    {
        clientNameSynchronizer.SetLocalClientName(name);
    }

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

    public void SetPlayer(Player player)
    {
        ClientData client = FindClient(player.OwnerClientId);
        if (client != null)
            client.player = player;
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
