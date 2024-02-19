using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class ClientNamesSynchronizer : NetworkBehaviour
{
    private static readonly string[] PLAYER_NAMES = new string[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel",
                                                                   "India", "Juliet", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec",
                                                                   "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "X-Ray", "Yankee", "Zulu" };

    GameState gameState;
    string thisClientName = "";

    private readonly Dictionary<ulong, List<ulong>> clientsSyncingNewClient = new();
    private readonly Dictionary<ulong, List<ulong>> newClientSyncingOldClients = new();
    private readonly List<ulong> waitingForInitialClientNameSync = new();

    public event System.Action OnClientsReady;

    void Start()
    {
        gameState = GameState.Instance;
        gameState.OnNewClientConnected += OnClientConnected;
    }

    private void SetupListsForNewClient(ulong clientId)
    {
        clientsSyncingNewClient.Add(clientId, new List<ulong>());
        newClientSyncingOldClients.Add(clientId, new List<ulong>());
    }

    private void OnClientConnected(ulong clientId)
    {
        // The server will start by adding new clients to a waiting list.
        // If the server is also this client, it will shortcut any list since it can sync
        // The client is responsible for syncing its own data to the server, then the server will sync the client's data to all other clients
        // The server will receive acknowledgement from the client that the data has been synced and remove the client from the waiting list
        // Currently, the only data that must be synced is the client's name and id

        SetupListsForNewClient(clientId);
        bool selfConnected = clientId == NetworkManager.Singleton.LocalClientId;

        if (IsServer)
        {
            if (!selfConnected)
            {
                // Add client to wait list. Wait for client to send their name using SyncMyClientNameToServerRpc()
                waitingForInitialClientNameSync.Add(clientId);
                SyncAllCurrentClientsToNewClient(clientId);
            }
            else
            {
                thisClientName = GetValidClientName(thisClientName);
                SetClientName(clientId, thisClientName);
            }
        }
        else if (selfConnected) // If this is the client that connected to server
        {
            Logger.Log($"Sending initial setup request to server with name {thisClientName}");
            RequestNameServerRpc(clientId, thisClientName);
        }
    }

    public void CheckAllClientsSynced()
    {
        if (IsServer)
        {
            int totalWaitingNewClients = 0;
            foreach (var client in clientsSyncingNewClient)
            {
                totalWaitingNewClients += client.Value.Count;
            }

            int totalWaitingOldClients = 0;
            foreach (var client in newClientSyncingOldClients)
            {
                totalWaitingOldClients += client.Value.Count;
            }

            if (waitingForInitialClientNameSync.Count <= 0 &&
                totalWaitingNewClients <= 0 &&
                totalWaitingOldClients <= 0)
            {
                Logger.Log("All clients synced. Ready to start game");
                OnClientsReady?.Invoke();
            }
        }
    }

    private void SetClientName(ulong clientId, string name)
    {
        ClientData client = gameState.GetClientData(clientId);
        if (client != null)
        {
            client.clientName = name;
        }
    }

    public void SetLocalClientName(string name)
    {
        thisClientName = name;
    }

    private string GetValidClientName(string playerName)
    {
        List<ClientData> connectedClients = gameState.GetConnectedClients();
        if (playerName == "")
        {
            do
            {
                playerName = PLAYER_NAMES[Random.Range(0, PLAYER_NAMES.Length)];
            } while (connectedClients.Exists(clientData => clientData.clientName == playerName));
        }

        return playerName;
    }

    private void SyncAllCurrentClientsToNewClient(ulong clientId)
    {
        List<ClientData> connectedClients = gameState.GetConnectedClients();
        string clientsToSync = "";
        foreach (var client in connectedClients)
        {
            if (client.clientId != clientId)
            {
                newClientSyncingOldClients[clientId].Add(client.clientId);
                clientsToSync += "{" + client.clientId + " : " + client.clientName + "}, ";
                SyncOldClientToNewClientRpc(client.clientId, client.clientName,
                                            Utility.SendToOneClient(clientId));
            }
        }
        Logger.Log($"Client [{clientId}] connected. Syncing all {connectedClients.Count - 1} current client(s): \n\t{clientsToSync}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNameServerRpc(ulong clientId, FixedString64Bytes name)
    {
        if (waitingForInitialClientNameSync.FindIndex(client => client == clientId) >= 0)
        {
            waitingForInitialClientNameSync.Remove(clientId);

            name = GetValidClientName(name.ToString());
            Logger.Log($"Syncing new client {clientId} with name {name}");

            if (!ClientIsMe(clientId))
                SetClientName(clientId, name.ToString());

            SyncClientNameToAll(clientId, name);
        }
        else
        {
            Logger.LogError($"Received name sync request from client {clientId} but client is not in waiting list");
        }
    }

    private void SyncClientNameToAll(ulong clientId, FixedString64Bytes name)
    {
        // Send this new client names to all other clients
        foreach (var client in gameState.GetConnectedClients())
        {
            if (!ClientIsMe(client))
                clientsSyncingNewClient[clientId].Add(client.clientId);
        }
        SyncNewNameClientRpc(clientId, name);
    }

    [ClientRpc]
    public void SyncNewNameClientRpc(ulong clientId, FixedString64Bytes playerName)
    {
        if (IsServer) return;

        Logger.Log($"Setting client name [{clientId}]: {playerName}");
        if (gameState.GetClientData(clientId) == null)
        {
            gameState.RegisterPlayer(clientId);
            // Logger.LogError($"Client {clientId} not found in connected clients list");
            // return;
        }

        SetClientName(clientId, playerName.ToString());

        if (ClientIsMe(clientId))
            SetLocalClientName(playerName.ToString());

        // Acknowledge to the server that this client has synced the data
        AcknowledgeNewNameServerRpc(NetworkManager.Singleton.LocalClientId, clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AcknowledgeNewNameServerRpc(ulong syncedClient, ulong dataFromClientId)
    {
        List<ulong> clients = clientsSyncingNewClient[dataFromClientId];
        if (clients != null)
        {
            clients.Remove(syncedClient);
            CheckAllClientsSynced();
        }
    }

    [ClientRpc]
    public void SyncOldClientToNewClientRpc(ulong clientId, FixedString64Bytes playerName, ClientRpcParams clientParams = default)
    {
        Logger.Log($"Syncing already connected client [{clientId}]: {playerName}");
        if (gameState.GetClientData(clientId) == null)
        {
            gameState.RegisterPlayer(clientId);
            // Logger.LogError($"Client {clientId} not found in connected clients list");
            // return;
        }

        SetClientName(clientId, playerName.ToString());
        // Acknowledge the server that this client has synced the data
        AcknowledgeOldClientsToNewServerRpc(NetworkManager.Singleton.LocalClientId, clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AcknowledgeOldClientsToNewServerRpc(ulong syncedClient, ulong dataFromClientId)
    {
        List<ulong> clients = newClientSyncingOldClients[syncedClient];
        if (clients != null)
        {
            Logger.Log($"Client {syncedClient} acknowledged by {dataFromClientId}");
            clients.Remove(dataFromClientId);
            CheckAllClientsSynced();
        }
    }

    private bool ClientIsMe(ClientData client)
    {
        return ClientIsMe(client.clientId);
    }

    private bool ClientIsMe(ulong clientId)
    {
        return clientId == NetworkManager.Singleton.LocalClientId;
    }
}
