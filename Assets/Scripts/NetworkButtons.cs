using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkButtons : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 500, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host"))
                NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client"))
                NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server"))
                NetworkManager.Singleton.StartServer();
        }

        // Buttons to change to windowed/fullscreen mode
        if (GUILayout.Button("Fullscreen"))
            Screen.fullScreen = true;
        if (GUILayout.Button("Windowed"))
            Screen.fullScreen = false;

        GUILayout.EndArea();
    }
}
