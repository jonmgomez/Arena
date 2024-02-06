using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkConnector : NetworkBehaviour
{
    public static NetworkConnector Instance { get; private set; }

    public event System.Action<ulong> OnSelfConnected;
    public event System.Action<ulong> OnOtherClientConnected;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnection;
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void OnConnection(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            OnSelfConnected?.Invoke(clientId);
        }
        else
        {
            OnOtherClientConnected?.Invoke(clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitialSetupServerRpc()
    {
        InitialSetupClientRpc();
    }

    [ClientRpc]
    public void InitialSetupClientRpc()
    {
        InitialSetupAcknowledgementServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitialSetupAcknowledgementServerRpc()
    {

    }
}
