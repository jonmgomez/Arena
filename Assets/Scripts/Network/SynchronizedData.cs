using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SynchronizedData : NetworkBehaviour
{
    protected Logger logger = new("SYNC");

    private readonly Dictionary<ulong, List<ulong>> syncingNewClient = new();
    private readonly Dictionary<ulong, List<ulong>> syncingCurrentClients = new();

    public event Action<ulong> OnWaitingForClient;
    public event Action<ulong> ClientSynced;
    public event Action AllClientsSynced;

    /// <summary>
    /// Register a client's id within the synchronization wait lists.
    /// Saves some trouble checking if the client is already in the list
    /// </summary>
    protected void AddNewClientId(ulong clientId)
    {
        syncingNewClient.Add(clientId, new List<ulong>());
        syncingCurrentClients.Add(clientId, new List<ulong>());
    }

    /// <summary>
    /// Checks if clients are synced and ready, if so invoke the AllClientsSynced event
    /// </summary>
    protected void CheckAllClientsSynced()
    {
        if (IsServer)
        {
            if (AreClientsSynced())
            {
                AllClientsSynced?.Invoke();
            }
        }
    }

    /// <summary>
    /// Check if a single client is synced and ready, if so invoke the ClientSynced event
    /// </summary>
    /// <param name="clientId">Id of client to check</param>
    protected virtual void CheckIfClientIsSynced(ulong clientId)
    {
        if (IsServer)
        {
            if (IsClientSynced(clientId))
            {
                logger.LogDebug($"Client {Utility.ClientNameToString(clientId)} is synced");
                ClientSynced?.Invoke(clientId);

                // And if this client has just been synced, check if all clients are now synced
                CheckAllClientsSynced();
            }
        }
    }

    /// <summary>
    /// Override this method to add additional checks to see if a client is synced
    /// </summary>
    protected virtual bool IsClientSyncedAdditional(ulong clientId)
    {
        return true;
    }

    /// <summary>
    /// Checks through all wait lists to see if this client has any other clients they are still waiting on for sync
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns>Boolean of whether this clients is in the process of syncing</returns>
    public bool IsClientSynced(ulong clientId)
    {
        return syncingNewClient.ContainsKey(clientId) && syncingNewClient[clientId].Count == 0 &&
               syncingCurrentClients.ContainsKey(clientId) && syncingCurrentClients[clientId].Count == 0 &&
               IsClientSyncedAdditional(clientId);
    }

    /// <summary>
    /// Override this method to add additional checks to see if all clients have synced
    /// </summary>
    protected virtual bool AreAllClientsSyncedAdditional()
    {
        return true;
    }

    /// <summary>
    /// Checks through all wait lists to see if there are any clients there are still being waited on for sync
    /// </summary>
    /// <returns>Whether all client's are in sync with each other</returns>
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

        return IsWaitingToSync(syncingNewClient) && IsWaitingToSync(syncingCurrentClients) && AreAllClientsSyncedAdditional();
    }

    /// <summary>
    /// Runs through all currently connected clients and adds their id to this clients list
    /// A ClientRpc with each client's name will be sent to this client
    /// </summary>
    /// <param name="clientId">The client to send all other clients data to</param>
    protected void SyncCurrentClientsToNewClient(ulong clientId, Action<ClientData> SyncCurrentClientRpcFunc)
    {
        List<ClientData> connectedClients = GameState.Instance.GetConnectedClients();
        foreach (var client in connectedClients)
        {
            if (client.clientId != clientId)
            {
                syncingCurrentClients[clientId].Add(client.clientId);
                SyncCurrentClientRpcFunc(client);
            }
        }

        OnWaitingForClient?.Invoke(clientId);
    }

    /// <summary>
    /// Acknowledge to the server that a past/current client's data has been received by this new client
    /// </summary>
    /// <param name="clientId">The id of the current client</param>
    /// <param name="syncedClient">The id of the new client which has acknowledged this data</param>
    [ServerRpc(RequireOwnership = false)]
    protected void AcknowledgeCurrentClientDataServerRpc(ulong clientId, ulong syncedClient)
    {
        logger.LogDebug($"New client {Utility.ClientNameToString(syncedClient)} has acknowledged current client {Utility.ClientNameToString(clientId)} data");

        syncingCurrentClients[syncedClient].Remove(clientId);
        CheckIfClientIsSynced(syncedClient);
    }

    /// <summary>
    /// Add all currently connected clients to the wait list. Send a ClientRpc to all clients with the new client's name
    /// <para>Clients will eventually acknowledge with a ServerRpc that the data was received</para>
    /// </summary>
    /// <param name="clientId">The id of the client in which their name will be sent</param>
    /// <param name="name">The name of this client to send to all others</param>
    protected void SyncNewClientToCurrentClients(ulong clientId, Action SyncNewClientRpcFunc)
    {
        List<ClientData> connectedClients = GameState.Instance.GetConnectedClients();
        foreach (var client in connectedClients)
        {
            if (!Net.IsLocalClient(client.clientId))
            {
                syncingNewClient[clientId].Add(client.clientId);
            }
        }
        SyncNewClientRpcFunc();

        OnWaitingForClient?.Invoke(clientId);
    }

    /// <summary>
    /// Acknowledge to the server that a new client's data has been received by this client
    /// </summary>
    /// <param name="clientId">The id of the new client</param>
    /// <param name="syncedClient">The id of the client that has received and acknowledged</param>
    [ServerRpc(RequireOwnership = false)]
    protected void AcknowledgeNewClientDataServerRpc(ulong clientId, ulong syncedClient)
    {
        logger.LogDebug($"Client {Utility.ClientNameToString(syncedClient)} has acknowledged new client {Utility.ClientNameToString(clientId)} data");

        syncingNewClient[clientId].Remove(syncedClient);
        CheckIfClientIsSynced(clientId);
    }
}
