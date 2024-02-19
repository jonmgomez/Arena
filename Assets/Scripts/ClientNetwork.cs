using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ClientNetwork : NetworkBehaviour
{
    public static ClientNetwork Instance { get; private set; }

    private readonly object clientListLock = new();
    private List<ClientData> connectedClients;

    private readonly Dictionary<ulong, List<ulong>> syncingNewClient = new();
    private readonly Dictionary<ulong, List<ulong>> syncingCurrentClients = new();

    public event Action<ulong> OnConnectToServer;
    // TODO: Provide callback when this client has disconnected from the server. Requires knowing who the server is
    // because the disconnect callback will give the server id, and not the local id -- public event Action<ulong> OnDisconnectToServer;
    public event Action<ulong> OnClientConnected;
    public event Action<ulong> OnClientConnectedAndReady;
    public event Action<ulong> OnClientDisconnected;

    public event Action OnClientsReady;

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
    }

    void Start()
    {
        connectedClients = GameState.Instance.GetConnectedClients();
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

    private void OnClientConnect(ulong clientId)
    {
        Logger.Log($"Client {Utility.ClientIdToString(clientId)} connected");
        RegisterPlayer(clientId);

        if (Net.IsLocalClient(clientId))
        {
            OnConnectToServer?.Invoke(clientId);
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

        OnClientDisconnected?.Invoke(clientId);
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
        OnClientDisconnected?.Invoke(clientId);
    }

    private bool IsClientSynced(ulong clientId)
    {
        return syncingCurrentClients[clientId].Count <= 0 && syncingNewClient[clientId].Count <= 0;
    }

    public bool AreClientsSynced()
    {
        static bool IsWaitingToSync(Dictionary<ulong, List<ulong>> clients)
        {
            foreach (var client in clients)
            {
                if (client.Value.Count > 0)
                    return false;
            }
            return true;
        }

        return IsWaitingToSync(syncingNewClient) && IsWaitingToSync(syncingCurrentClients);
    }

    private void CheckIfNewClientIsSynced(ulong clientId)
    {
        if (IsClientSynced(clientId))
        {
            Logger.Log($"Client {Utility.ClientNameToString(clientId)} is synced");
            OnClientConnectedAndReady?.Invoke(clientId);
        }

        if (AreClientsSynced())
        {
            Logger.Log("All clients are network synced");
            OnClientsReady?.Invoke();
        }
    }
}
