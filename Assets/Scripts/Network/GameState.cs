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
    private readonly Logger logger = new("GAMESTATE");

    public static GameState Instance { get; private set; }

    private readonly List<ClientData> connectedClients = new();
    private readonly List<ulong> waitingClients = new();
    private ClientNetwork clientNetwork;
    private ClientNamesSynchronizer clientNameSynchronizer;

    public event Action<ulong> ClientReady;
    public event Action AllClientsReady;
    public event Action WaitingForClients;

    float timeSinceGameStart = 0f;
    bool clientsReady = true;

    // Is this is a in-game scene or a lobby/menu
    bool inGameScene = false;

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadComplete;
        }
        else
        {
            logger.LogError("NetworkManager.Singleton is null!");
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        clientNetwork = GetComponent<ClientNetwork>();
        clientNetwork.ClientSynced += ClientNetworkSynced;
        clientNetwork.WaitingForClient += ClientsNoLongerReady;
        clientNetwork.OnConnectToServer += (clientId) =>
        {
            OnClientReady(clientId);
        };

        clientNameSynchronizer = GetComponent<ClientNamesSynchronizer>();
        clientNameSynchronizer.ClientSynced += ClientNamesSynced;
        clientNameSynchronizer.WaitingForClient += ClientsNoLongerReady;
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
        logger.Log($"Networked load scene complete. {sceneName} loaded. {clientsCompleted.Count} clients connected. {clientsTimedOut.Count} clients timed out.");

        if (IsServer)
        {
            ValidateScene(sceneName);

            if (inGameScene)
            {
                foreach (ulong client in clientsCompleted)
                {
                    SpawnClientPlayerPrefab(client);
                }
            }
        }
    }

    private void ClientsNoLongerReady(ulong clientId)
    {
        if (clientsReady)
        {
            clientsReady = false;
            WaitingForClients?.Invoke();
        }
    }

    private void ClientNetworkSynced(ulong clientId)
    {
        CheckClientReady(clientId);
    }

    private void ClientNamesSynced(ulong clientId)
    {
        CheckClientReady(clientId);
    }

    /// <summary>
    /// Checks if this client is present in any synchronizing components wait list
    /// If not, call events notifying that this client is ready
    /// </summary>
    public void CheckClientReady(ulong clientId)
    {
        if (IsServer && !clientsReady)
        {
            if (clientNetwork.IsClientSynced(clientId) && clientNameSynchronizer.IsClientSynced(clientId))
            {
                OnClientReady(clientId);
                ClientReady?.Invoke(clientId);

                CheckAllClientsReady();
            }
        }
    }

    /// <summary>
    /// Checks if there are any waiting clients from any synchronizing components
    /// Will call the AllClientsReady event if there are no waiting clients
    /// </summary>
    private void CheckAllClientsReady()
    {
        if (clientNetwork.AreClientsSynced() && clientNameSynchronizer.AreClientsSynced())
        {
            logger.Log("All clients are ready. Game is able to start");
            AllClientsReady?.Invoke();
            clientsReady = true;
        }
    }

    /// <summary>
    /// Called when a client has successfully synced all necessary data.
    /// This will spawn a player prefab if the game has loaded an in-game scene
    /// </summary>
    /// <param name="clientId"></param>
    public void OnClientReady(ulong clientId)
    {
        if (IsServer)
        {
            if (inGameScene)
            {
                SpawnClientPlayerPrefab(clientId);
            }
        }
    }

    private void SpawnClientPlayerPrefab(ulong clientId)
    {
        if (IsServer)
        {
            ClientData clientData = FindClient(clientId);
            if (clientData != null)
            {
                Player spawnedPlayer = PlayerSpawnController.Instance.SpawnNewPlayerPrefab(clientId);
                clientData.player = spawnedPlayer;
            }
            else
            {
                logger.LogError($"Cannot spawn player for client {clientId}. Client not found in connections list");
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
