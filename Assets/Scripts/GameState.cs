using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameState : NetworkBehaviour
{
    private class ClientData
    {
        public ulong clientId;
        public string clientName;
        public Player player;
    }

    public static GameState Instance { get; private set; }
    private static readonly string[] PLAYER_NAMES = new string[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel",
                                                            "India", "Juliet", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec",
                                                            "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "X-Ray", "Yankee", "Zulu" };

    string thisClientName = "";
    List<ClientData> connectedClients = new List<ClientData>();
    List<ulong> waitingForClients = new List<ulong>();

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
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadComplete;
        }
        else
        {
            Logger.LogError("NetworkManager.Singleton is null!");
        }
    }

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ValidateScene(SceneManager.GetActiveScene().name);
    }

    private void ValidateScene(string sceneName)
    {
        // Any scenes where a player must be spawned for clients are prefixed with "Arena"
        inGameScene = sceneName.StartsWith("Arena");
    }

    private void RegisterPlayer(ulong clientId)
    {
        connectedClients.Add(new ClientData { clientId = clientId, player = null });
    }

    private void UnregisterPlayer(ulong clientId)
    {
        connectedClients.Remove(connectedClients.Find(clientData => clientData.clientId == clientId));
    }

    private ClientData FindClient(ulong clientId)
    {
        return connectedClients.Find(clientData => clientData.clientId == clientId);
    }

    #region NetworkEventCallbacks
    private void OnClientConnected(ulong clientId)
    {
        bool selfConnected = clientId == NetworkManager.Singleton.LocalClientId;
        Logger.Log($"Client [{clientId}] " + (selfConnected ? "(Self) " : "") + "connected");

        RegisterPlayer(clientId);

        // The server will start by adding new clients to a waiting list.
        // If the server is also this client, it will shortcut any list since it can sync
        // The client is responsible for syncing its own data to the server, then the server will sync the client's data to all other clients
        // The server will receive acknowledgement from the client that the data has been synced and remove the client from the waiting list
        // Currently, the only data that must be synced is the client's name and id
        if (IsServer)
        {
            if (!selfConnected)
            {
                waitingForClients.Add(clientId);

                // Sync all current clients to the new client
                var clientParams = Utility.CreateClientRpcParams(clientId);
                string clientsToSync = "";
                for (int i = 0; i < connectedClients.Count; i++)
                {
                    if (connectedClients[i].clientId != clientId)
                    {
                        clientsToSync += "{" + connectedClients[i].clientId + " : " + connectedClients[i].clientName + "}, ";
                        SetClientNameClientRpc(connectedClients[i].clientId, connectedClients[i].clientName, clientParams);
                    }
                }
                Logger.Log($"Client [{clientId}] connected. Syncing all {connectedClients.Count} current clients: \n{clientsToSync}");

            }
            else
            {
                thisClientName = GetPlayerName(thisClientName);
                SetClientName(clientId, thisClientName);
                ClientReady(clientId);
            }
        }
        else if (selfConnected)
        {
            Logger.Log($"Sending initial setup request to server with name {thisClientName}");
            SyncNewClientToServerRpc(clientId, thisClientName);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Logger.Log($"Client [{clientId}] disconnected");
        UnregisterPlayer(clientId);

        if (IsServer)
        {
            ClientDisconnectedClientRpc(clientId);
        }
    }

    [ClientRpc]
    public void ClientDisconnectedClientRpc(ulong clientId)
    {
        Logger.Log($"Client [{clientId}] disconnected");
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

    [ServerRpc(RequireOwnership = false)]
    public void SyncNewClientToServerRpc(ulong clientId, FixedString64Bytes playerName)
    {
        playerName = GetPlayerName(playerName.ToString());

        Logger.Log($"Syncing new client {clientId} with name {playerName}");
        FindClient(clientId).clientName = playerName.ToString();

        SetClientNameClientRpc(clientId, playerName);
    }

    private string GetPlayerName(string playerName)
    {
        if (playerName == "")
        {
            do
            {
                playerName = PLAYER_NAMES[UnityEngine.Random.Range(0, PLAYER_NAMES.Length)];
            } while (connectedClients.Exists(clientData => clientData.clientName == playerName));
        }

        return playerName;
    }

    [ClientRpc]
    public void SetClientNameClientRpc(ulong clientId, FixedString64Bytes playerName, ClientRpcParams clientParams = default)
    {
        Logger.Log($"Setting client name for {clientId} to {playerName}");
        ClientData client = FindClient(clientId);
        if (client == null)
        {
            client = new ClientData { clientId = clientId, clientName = playerName.ToString() };
            connectedClients.Add(client);
        }

        client.clientName = playerName.ToString();

        // Send an acknowledgement to the server that the client has synced the data
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetLocalClientName(playerName.ToString());
            AcknowledgeSyncedDataServerRpc(clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AcknowledgeSyncedDataServerRpc(ulong clientId)
    {
        bool found = waitingForClients.Remove(clientId);
        if (!found)
            Logger.LogError($"Client {clientId} not found in waiting list");
        else
            Logger.Log($"Client {clientId} acknowledged and synced successfully");

        ClientReady(clientId);
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

    private void SetClientName(ulong clientId, string playerName)
    {
        ClientData client = FindClient(clientId);
        if (client != null)
        {
            client.clientName = playerName;
        }
    }

    public void SetLocalClientName(string playerName)
    {
        thisClientName = playerName;
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
}
