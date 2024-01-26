using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button serverButton;
    [SerializeField] Button clientButton;
    [SerializeField] Button startButton;
    [SerializeField] GameObject clientConnectingMessage;
    [SerializeField] GameObject clientWaitMessage;
    [SerializeField] TextMeshProUGUI serverClientsConnectedText;

    void Start()
    {
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            EnableServerInterface();
        });

        serverButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            EnableServerInterface();
        });

        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            EnableClientInterface();
        });

        startButton.onClick.AddListener(() => {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        });
    }

    private void EnableServerInterface()
    {
        hostButton.gameObject.SetActive(false);
        serverButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);

        startButton.gameObject.SetActive(true);
        serverClientsConnectedText.gameObject.SetActive(true);

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
            Debug.Log($"[SERVER] Client connected (client id: {clientId})");
            serverClientsConnectedText.text = $"Players connected: {NetworkManager.Singleton.ConnectedClientsList.Count}";
        };
    }

    private void EnableClientInterface()
    {
        hostButton.gameObject.SetActive(false);
        serverButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);

        clientConnectingMessage.SetActive(true);
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
            Debug.Log($"[CLIENT] Connected to server (client id: {clientId} {NetworkManager.Singleton.LocalClientId})");
            EnableClientConnectedInterface();
        };
    }

    private void EnableClientConnectedInterface()
    {
        clientConnectingMessage.SetActive(false);
        clientWaitMessage.SetActive(true);
    }
}
