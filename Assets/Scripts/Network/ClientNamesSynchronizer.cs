using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class ClientNamesSynchronizer : SynchronizedData
{
    private static readonly string[] PLAYER_NAMES = new string[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel",
                                                                   "India", "Juliet", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec",
                                                                   "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "X-Ray", "Yankee", "Zulu" };

    GameState gameState;
    string thisClientName = "";

    private readonly List<ulong> waitingForInitialClientNameSync = new();

    public ClientNamesSynchronizer()
    {
        logger = new("NAMES");
    }

    void Start()
    {
        gameState = GameState.Instance;
        ClientNetwork.Instance.OnClientConnected += OnClientConnected;
        ClientNetwork.Instance.OnConnectToServer += OnSelfConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // The server will start by adding new clients to a waiting list.
        // If the server is also this client, it will shortcut any list since it can sync
        // The client is responsible for syncing its own data to the server, then the server will sync the client's data to all other clients
        // The server will receive acknowledgement from the client that the data has been synced and remove the client from the waiting list
        // Currently, the only data that must be synced is the client's name and id
        if (IsServer)
        {
            waitingForInitialClientNameSync.Add(clientId);
            AddNewClientId(clientId);

            SyncCurrentClientsToNewClient(clientId, (ClientData client) => {
                SyncCurrentClientToNewClientRpc(client.clientId, client.clientName,
                                                Utility.SendToOneClient(clientId));
            });
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

    protected override bool IsClientSyncedAdditional(ulong clientId)
    {
        return !waitingForInitialClientNameSync.Contains(clientId);
    }

    protected override bool AreAllClientsSyncedAdditional()
    {
        return waitingForInitialClientNameSync.Count == 0;
    }

    /// <summary>
    /// Request a name for this client id to the server. The server will respond by syncing a
    /// new valid (unchanged is valid) name to this client and all other clients
    /// </summary>
    /// <param name="clientId">The id of the client name to request for</param>
    /// <param name="name"></param>
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


            SyncNewClientToCurrentClients(clientId, () => {
                SyncNewNameClientRpc(clientId, name);
            });
        }
        else
        {
            logger.LogError($"Received name sync request from client {Utility.ClientIdToString(clientId)}, but client is not in waiting list");
        }
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
        AcknowledgeNewClientDataServerRpc(clientId, Net.LocalClientId);
    }

    [ClientRpc]
    public void SyncCurrentClientToNewClientRpc(ulong clientId, FixedString64Bytes playerName, ClientRpcParams clientParams = default)
    {
        if (gameState.GetClientData(clientId) == null)
        {
            logger.LogError($"Client {clientId} not found in connected clients list");
            return;
        }

        logger.Log($"Setting current client name {Utility.ClientIdToString(clientId)}: {playerName}");
        SetClientName(clientId, playerName.ToString());

        // Acknowledge the server that this client has synced the data
        AcknowledgeCurrentClientDataServerRpc(clientId, Net.LocalClientId);
    }
}
