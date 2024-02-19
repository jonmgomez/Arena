using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public static class Net
{
    public static bool IsServer => NetworkManager.Singleton.IsServer;
    public static bool IsClient => NetworkManager.Singleton.IsClient;
    public static bool IsHost => NetworkManager.Singleton.IsHost;

    public static bool IsServerOnly => NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient;
    public static bool IsClientOnly => NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer;

    public static ulong LocalClientId => NetworkManager.Singleton.LocalClientId;
    public static bool IsLocalClient(ulong clientId) => clientId == LocalClientId;
}
