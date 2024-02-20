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
    private readonly Logger logger = new("NAMES");

    GameState gameState;
    string thisClientName = "";

    private readonly Dictionary<ulong, List<ulong>> syncingNewClient = new();
    private readonly Dictionary<ulong, List<ulong>> syncingCurrentClients = new();
    private readonly List<ulong> waitingForInitialClientNameSync = new();

    public event System.Action OnClientsReady;

    void Start()
    {
        gameState = GameState.Instance;
        ClientNetwork.Instance.OnClientConnected += OnClientConnected;
        ClientNetwork.Instance.OnConnectToServer += OnSelfConnected;
    }

    private void SetupListsForNewClient(ulong clientId)
    {
        syncingNewClient.Add(clientId, new List<ulong>());
        syncingCurrentClients.Add(clientId, new List<ulong>());
    }

    private void OnClientConnected(ulong clientId)
    {
        // The server will start by adding new clients to a waiting list.
        // If the server is also this client, it will shortcut any list since it can sync
        // The client is responsible for syncing its own data to the server, then the server will sync the client's data to all other clients
        // The server will receive acknowledgement from the client that the data has been synced and remove the client from the waiting list
        // Currently, the only data that must be synced is the client's name and id

        SetupListsForNewClient(clientId);
        if (IsServer)
        {
            // Add client to wait list. Wait for client to send their name using SyncMyClientNameToServerRpc()
            waitingForInitialClientNameSync.Add(clientId);
            SyncAllCurrentClientsToNewClient(clientId);
        }
    }

    private void OnSelfConnected(ulong clientId)
    {
        if (IsServer)
        {
            thisClientName = GetValidClientName(thisClientName);
            SetClientName(clientId, thisClientName);
        }
        else
        {
            logger.Log($"Sending initial setup request to server with name {thisClientName}");
            RequestNameServerRpc(clientId, thisClientName);
        }
    }

    /// <summary>
    /// Checks if clients are synced and ready, if so invoke the OnClientsReady event
    /// </summary>
    public void CheckAllClientsSynced()
    {
        if (IsServer)
        {
            if (AreClientsSynced())
            {
                logger.Log("Client names synced");
                OnClientsReady?.Invoke();
            }
        }
    }

    /// <summary>
    /// Checks through all wait lists to see if there are any clients there are still being waited on for sync
    /// </summary>
    /// <returns>Whether all client's names are in sync with each other</returns>
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

        return waitingForInitialClientNameSync.Count <= 0 &&
               IsWaitingToSync(syncingNewClient) &&
               IsWaitingToSync(syncingCurrentClients);
    }

    /// <summary>
    /// Edit the clients name in the client list
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="name"></param>
    private void SetClientName(ulong clientId, string name)
    {
        ClientData client = gameState.GetClientData(clientId);
        if (client != null)
        {
            client.clientName = name;
        }
    }

    /// <summary>
    /// Sets the local client name. This is for use when a initial connection is made.
    /// This local name will be sent to the server as a request and a valid name will be returned (The name may be unchanged if already valid)
    /// </summary>
    public void SetLocalClientName(string name)
    {
        thisClientName = name;
    }

    /// <summary>
    /// Returns a valid client name. If the name is empty, a "random" name will be chosen
    /// </summary>
    /// <param name="playerName">A name to check, will be unchanged if already valid</param>
    /// <returns>The valid name. Will return the param string if no changes necessary</returns>
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

    /// <summary>
    /// Runs through all currently connected clients and adds their id to this clients list
    /// A ClientRpc with each client's name will be sent to this client
    /// </summary>
    /// <param name="clientId">The client to send all other clients data to</param>
    private void SyncAllCurrentClientsToNewClient(ulong clientId)
    {
        List<ClientData> connectedClients = gameState.GetConnectedClients();
        foreach (var client in connectedClients)
        {
            if (client.clientId != clientId)
            {
                syncingCurrentClients[clientId].Add(client.clientId);
                SyncOldClientToNewClientRpc(client.clientId, client.clientName,
                                            Utility.SendToOneClient(clientId));
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNameServerRpc(ulong clientId, FixedString64Bytes name)
    {
        if (waitingForInitialClientNameSync.FindIndex(client => client == clientId) >= 0)
        {
            waitingForInitialClientNameSync.Remove(clientId);

            name = GetValidClientName(name.ToString());
            logger.Log($"Syncing client {Utility.ClientIdToString(clientId)} with name {name}");

            if (!Net.IsLocalClient(clientId))
                SetClientName(clientId, name.ToString());

            SyncClientNameToAll(clientId, name);
        }
        else
        {
            logger.LogError($"Received name sync request from client {Utility.ClientIdToString(clientId)}, but client is not in waiting list");
        }
    }

    /// <summary>
    /// Add all currently connected clients to the wait list. Send a ClientRpc to all clients with the new client's name
    /// <para>Clients will eventually acknowledge with a ServerRpc that the data was received</para>
    /// </summary>
    /// <param name="clientId">The id of the client in which their name will be sent</param>
    /// <param name="name">The name of this client to send to all others</param>
    private void SyncClientNameToAll(ulong clientId, FixedString64Bytes name)
    {
        // Send this new client names to all other clients
        foreach (var client in gameState.GetConnectedClients())
        {
            if (!Net.IsLocalClient(client.clientId))
                syncingNewClient[clientId].Add(client.clientId);
        }
        SyncNewNameClientRpc(clientId, name);
    }

    [ClientRpc]
    public void SyncNewNameClientRpc(ulong clientId, FixedString64Bytes playerName)
    {
        if (IsServer) return;

        logger.Log($"Setting client name {Utility.ClientIdToString(clientId)}: {playerName}");
        if (gameState.GetClientData(clientId) == null)
        {
            logger.LogError($"Client {clientId} not found in connected clients list");
            return;
        }

        SetClientName(clientId, playerName.ToString());

        if (Net.IsLocalClient(clientId))
            SetLocalClientName(playerName.ToString());

        // Acknowledge to the server that this client has synced the data
        AcknowledgeNewNameServerRpc(NetworkManager.Singleton.LocalClientId, clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AcknowledgeNewNameServerRpc(ulong syncedClient, ulong dataFromClientId)
    {
        List<ulong> clients = syncingNewClient[dataFromClientId];
        if (clients != null)
        {
            clients.Remove(syncedClient);
            CheckAllClientsSynced();
        }
    }

    [ClientRpc]
    public void SyncOldClientToNewClientRpc(ulong clientId, FixedString64Bytes playerName, ClientRpcParams clientParams = default)
    {
        if (gameState.GetClientData(clientId) == null)
        {
            logger.LogError($"Client {clientId} not found in connected clients list");
            return;
        }

        SetClientName(clientId, playerName.ToString());
        // Acknowledge the server that this client has synced the data
        AcknowledgeOldClientsToNewServerRpc(NetworkManager.Singleton.LocalClientId, clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AcknowledgeOldClientsToNewServerRpc(ulong syncedClient, ulong dataFromClientId)
    {
        List<ulong> clients = syncingCurrentClients[syncedClient];
        if (clients != null)
        {
            clients.Remove(dataFromClientId);
            CheckAllClientsSynced();
        }
    }
}
