using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ClientNetwork : SynchronizedData
{
    public static ClientNetwork Instance { get; private set; }

    private readonly object clientListLock = new();
    private List<ClientData> connectedClients;

    public event Action<ulong> OnConnectToServer;
    // TODO: Provide callback when this client has disconnected from the server. Requires knowing who the server is
    // because the disconnect callback will give the server id, and not the local id -- public event Action<ulong> OnDisconnectToServer;
    public event Action<ulong> OnClientConnected;
    public event Action<ulong> OnClientConnectedAndReady;
    public event Action<ulong> OnClientDisconnected;

    public ClientNetwork()
    {
        logger = new("CLNET");
    }

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
            logger.LogError("NetworkManager.Singleton is null!");
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

        ClientSynced += (clientId) => OnClientConnectedAndReady?.Invoke(clientId);
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
        logger.Log($"Client {Utility.ClientIdToString(clientId)} connected");
        RegisterPlayer(clientId);

        if (Net.IsLocalClient(clientId))
        {
            OnConnectToServer?.Invoke(clientId);
        }
        else if (IsServer)
        {
            AddNewClientId(clientId);

            string clientsToSync = "";
            SyncCurrentClientsToNewClient(clientId, (ClientData client) => {
                clientsToSync += "{" + client.clientId + " : " + client.clientName + "}, ";
                CurrentClientInformationClientRpc(client.clientId, Utility.SendToOneClient(clientId));
            });
            logger.Log($"Syncing all {connectedClients.Count - 1} preexisting clients to new client {Utility.ClientIdToString(clientId)}\ncurrent client(s): {clientsToSync}");

            SyncNewClientToCurrentClients(clientId, () => {
                NewClientInformationClientRpc(clientId);
            });

            OnClientConnected?.Invoke(clientId);
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        logger.Log($"Client {Utility.ClientNameToString(clientId)} disconnected");
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
        if (IsServer)
            return;

        if (!Net.IsLocalClient(clientId))
        {
            logger.Log($"New client {Utility.ClientIdToString(clientId)} connected. Information received from server. Acknowledging...");
            RegisterPlayer(clientId);

            OnClientConnected?.Invoke(clientId);
        }

        AcknowledgeNewClientDataServerRpc(clientId, Net.LocalClientId);
    }

    [ClientRpc]
    private void CurrentClientInformationClientRpc(ulong clientId, ClientRpcParams clientParams = default)
    {
        logger.Log($"Received information about a preexisting client {Utility.ClientIdToString(clientId)} from the server. Acknowledging...");
        RegisterPlayer(clientId);

        AcknowledgeCurrentClientDataServerRpc(clientId, Net.LocalClientId);
    }

    [ClientRpc]
    public void ClientDisconnectedClientRpc(ulong clientId)
    {
        if (IsServer)
            return;

        logger.Log($"Client {Utility.ClientIdToString(clientId)} disconnected");
        UnregisterPlayer(clientId);
        OnClientDisconnected?.Invoke(clientId);
    }
}
