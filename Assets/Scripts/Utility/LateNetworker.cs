using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used when starting the game from a game scene.
/// <para>It will enable the abililty to connect to the network without having to start the game from the main menu.</para>
/// </summary>
public class LateNetworker : MonoBehaviour
{
    [SerializeField] NetworkManager networkManagerPrefab;
    [SerializeField] PlayerSpawnController playerSpawnController;
    [SerializeField] Button hostButton;
    [SerializeField] Button serverButton;
    [SerializeField] Button clientButton;

    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            gameObject.SetActive(false);
            return;
        }

        Logger.Default.Log("Instantiating NetworkManager");
        Instantiate(networkManagerPrefab);

        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            gameObject.SetActive(false);
        });

        serverButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            gameObject.SetActive(false);
        });

        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            gameObject.SetActive(false);
        });
    }
}
