using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
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
    private ClientNetwork clientNetwork;
    private ClientNamesSynchronizer clientNameSynchronizer;

    public event Action<ulong> ClientReady;
    public event Action AllClientsReady;
    public event Action WaitingForClients;

    bool clientsReady = true;

    // Is this is a in-game scene or a lobby/menu
    bool inGameScene = false;
    string currentScene = "";

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
        clientNetwork.OnClientDisconnected += OnClientDisconnect;
        clientNetwork.OnSelfDisconnect += (bool wasServer) =>
        {
            connectedClients.Clear();
            clientNetwork.Reset();
            clientNameSynchronizer.Reset();
        };

        clientNameSynchronizer = GetComponent<ClientNamesSynchronizer>();
        clientNameSynchronizer.ClientSynced += ClientNamesSynced;
        clientNameSynchronizer.WaitingForClient += ClientsNoLongerReady;
    }

    void Start()
    {
        ValidateScene(SceneManager.GetActiveScene().name);
    }

    private void ValidateScene(string sceneName)
    {
        // Any scenes where a player must be spawned for clients are prefixed with "Arena"
        inGameScene = sceneName.StartsWith("Arena") || sceneName.StartsWith("Prototype");
    }

    private ClientData FindClient(ulong clientId)
    {
        return connectedClients.Find(clientData => clientData.clientId == clientId);
    }

    private void OnNetworkSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode,
                                            List<ulong> clientsCompleted,
                                            List<ulong> clientsTimedOut)
    {
        if (currentScene == sceneName) return;
        currentScene = sceneName;

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
                logger.Log($"Client {clientId} is ready");
                ClientReady?.Invoke(clientId);
                OnClientReady(clientId);

                CheckAllClientsReady();

                InformClientIsSyncedClientRpc();
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
            else
            {
                logger.LogDebug($"Client {clientId} is ready. Not spawning player prefab as this is not an in-game scene");
            }
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (inGameScene) // Clients only have a player prefab in in-game scenes
            DespawnClientPlayerPrefab(clientId);
        else
            RemoveClient(clientId);
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

                InGameController.Instance.PlayerSpawned(spawnedPlayer);
            }
            else
            {
                logger.LogError($"Cannot spawn player for client {clientId}. Client not found in connections list");
            }
        }
    }

    private void DespawnClientPlayerPrefab(ulong clientId)
    {
        // Unity will automatically despawn the player prefab.
        // So this is to cleanup any other data that needs to be removed
        InGameController.Instance.PlayerDespawned(clientId);
    }

    /// <summary>
    /// Send message to a specific client that it has properly synced necessary data
    /// </summary>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    private void InformClientIsSyncedClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return; // Server has already notified itself
        ClientReady?.Invoke(Net.LocalClientId);
    }

    #region Setters/Getters
    public void SetLocalClientName(string name)
    {
        clientNameSynchronizer.SetLocalClientName(name);
    }

    private void RemoveClient(ulong clientId)
    {
        ClientData client = FindClient(clientId);
        if (client != null)
            connectedClients.Remove(client);
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

    /// <summary>
    /// Get the local player for the client (The player that the client controls)
    /// </summary>
    /// <returns>Local Player instance</returns>
    public Player GetLocalPlayer()
    {
        return GetPlayer(Net.LocalClientId);
    }

    public List<Player> GetPlayers()
    {
        List<Player> players = new();

        foreach (ClientData client in connectedClients)
        {
            if (client.player != null)
                players.Add(client.player);
        }

        return players;
    }
    #endregion
}
