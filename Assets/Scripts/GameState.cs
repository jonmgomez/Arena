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

    private readonly object clientListLock = new();
    private readonly List<ClientData> connectedClients = new List<ClientData>();
    private ClientNamesSynchronizer clientNameSynchronizer;

    private readonly Dictionary<ulong, List<ulong>> syncingNewClient = new();
    private readonly Dictionary<ulong, List<ulong>> syncingCurrentClients = new();

    public event Action<ulong> OnSelfConnected;
    public event Action<ulong> OnClientConnected;
    public event Action<ulong> OnClientConnectedAndReady;
    public event Action<ulong> OnClientDisconnected;

    public event Action AllClientsReady;

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
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
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

    public void RegisterPlayer(ulong clientId)
    {
        lock (clientListLock)
        {
            connectedClients.Add(new ClientData { clientId = clientId, player = null });
        }
    }

    private void UnregisterPlayer(ulong clientId)
    {
        lock (clientListLock)
        {
            connectedClients.Remove(connectedClients.Find(clientData => clientData.clientId == clientId));
        }
    }

    private ClientData FindClient(ulong clientId)
    {
        return connectedClients.Find(clientData => clientData.clientId == clientId);
    }

    #region NetworkEventCallbacks
    private void OnClientConnect(ulong clientId)
    {
        Logger.Log($"Client {Utility.ClientIdToString(clientId)} connected");
        RegisterPlayer(clientId);

        if (Net.IsLocalClient(clientId))
        {
            OnSelfConnected?.Invoke(clientId);
        }
        else if (IsServer)
        {
            lock (clientListLock)
            {
                // Send information about all already connected clients to the new client
                string clientsToSync = "";
                syncingCurrentClients.Add(clientId, new List<ulong>());
                foreach (ClientData client in connectedClients)
                {
                    if (client.clientId == clientId)
                        continue;

                    clientsToSync += "{" + client.clientId + " : " + client.clientName + "}, ";
                    syncingCurrentClients[clientId].Add(client.clientId);
                    CurrentClientInformationClientRpc(client.clientId,
                                                      Utility.SendToOneClient(clientId));
                }
                Logger.Log($"Syncing all {connectedClients.Count - 1} preexisting clients to new client {Utility.ClientIdToString(clientId)}\ncurrent client(s): {clientsToSync}");

                // Send information about the new client to all already connected clients
                // (except the new client and the server)
                syncingNewClient.Add(clientId, new List<ulong>());
                foreach (ClientData client in connectedClients)
                {
                    if (client.clientId == clientId || Net.IsLocalClient(client.clientId))
                        continue;

                    syncingNewClient[clientId].Add(client.clientId);
                }
                NewClientInformationClientRpc(clientId);
            }

            OnClientConnected?.Invoke(clientId);
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Logger.Log($"Client {Utility.ClientNameToString(clientId)} disconnected");
        UnregisterPlayer(clientId);

        if (IsServer)
        {
            // Client disconnects do not require syncing (response/acknowledgment) for the time being
            ClientDisconnectedClientRpc(clientId);
        }
    }

    [ClientRpc]
    private void NewClientInformationClientRpc(ulong clientId)
    {
        if (IsServer || clientId == Net.LocalClientId)
            return;

        Logger.Log($"New client {Utility.ClientIdToString(clientId)} connected. Information received from server. Acknowledging...");
        RegisterPlayer(clientId);
        OnClientConnected?.Invoke(clientId);

        AcknowledgeNewClientInformationServerRpc(clientId, Net.LocalClientId);
    }

    // Clients that are not the server should acknowledge that they are aware
    // of a new client connecting to the server
    [ServerRpc(RequireOwnership = false)]
    public void AcknowledgeNewClientInformationServerRpc(ulong clientId, ulong syncedClient)
    {
        syncingNewClient[clientId].Remove(syncedClient);
        CheckIfNewClientIsSynced(clientId);
    }

    [ClientRpc]
    private void CurrentClientInformationClientRpc(ulong clientId, ClientRpcParams clientParams = default)
    {
        Logger.Log($"Received information about a preexisting client {Utility.ClientIdToString(clientId)} from the server. Acknowledging...");
        RegisterPlayer(clientId);
        AcknowledgeCurrentClientInformationServerRpc(clientId, Net.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AcknowledgeCurrentClientInformationServerRpc(ulong clientId, ulong syncedClient)
    {
        // Note that ordering is swapped here because the new client must acknowledge ALL OTHER clients.
        syncingCurrentClients[syncedClient].Remove(clientId);
        CheckIfNewClientIsSynced(syncedClient);
    }

    [ClientRpc]
    public void ClientDisconnectedClientRpc(ulong clientId)
    {
        if (IsServer)
            return;

        Logger.Log($"Client {Utility.ClientIdToString(clientId)} disconnected");
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

    private void ClientNamesSynced()
    {
        if (IsServer)
        {
            if (AreClientsSynced())
            {
                ClientsReady();
            }
        }
    }

    private bool IsClientSynced(ulong clientId)
    {
        return syncingCurrentClients[clientId].Count <= 0 && syncingNewClient[clientId].Count <= 0;
    }

    public bool AreClientsSynced()
    {
        int totalWaitingNewClients = 0;
        foreach (var client in syncingNewClient)
        {
            totalWaitingNewClients += client.Value.Count;
        }

        int totalWaitingOldClients = 0;
        foreach (var client in syncingCurrentClients)
        {
            totalWaitingOldClients += client.Value.Count;
        }

        return totalWaitingNewClients <= 0 && totalWaitingOldClients <= 0;
    }

    private void CheckIfNewClientIsSynced(ulong clientId)
    {
        if (IsClientSynced(clientId))
        {
            Logger.Log($"Client {Utility.ClientNameToString(clientId)} is fully synced");
            OnClientConnectedAndReady?.Invoke(clientId);
        }

        if (AreClientsSynced())
        {
            if (clientNameSynchronizer.AreClientsSynced())
            {
                ClientsReady();
            }
        }
    }

    private void ClientsReady()
    {
        Logger.Log("All clients are ready. Game is able to start");
        AllClientsReady?.Invoke();
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

    public Player GetPlayer(ulong clientId)
    {
        ClientData client = FindClient(clientId);
        if (client != null)
            return client.player;

        return null;
    }
    #endregion
}
