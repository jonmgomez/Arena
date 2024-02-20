using System;
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
    [SerializeField] RelayManager relay;
    [SerializeField] Button hostButton;
    [SerializeField] Button serverButton;
    [SerializeField] Button clientButton;
    [SerializeField] Button startButton;
    [SerializeField] GameObject clientConnectingMessage;
    [SerializeField] GameObject clientWaitMessage;
    [SerializeField] GameObject serverCreatingMessage;
    [SerializeField] TextMeshProUGUI serverClientsConnectedText;
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] TMP_InputField joinCodeText;
    [SerializeField] TMP_InputField playerNameInput;
    [SerializeField] TextMeshProUGUI waitingForClientsText;

    void Start()
    {
        hostButton.onClick.AddListener(() => {
            if (relay.UsingRelay())
            {
                relay.OnJoinCodeGenerated += (joinCode) => {
                    EnableServerReadyInterface(joinCode);
                };
                relay.CreateRelay();
            }
            else
            {
                NetworkManager.Singleton.StartHost();
            }

            EnableServerInterface();
        });

        serverButton.onClick.AddListener(() => {
            if (relay.UsingRelay())
            {
                relay.OnJoinCodeGenerated += (joinCode) => {
                    EnableServerReadyInterface(joinCode);
                };
                relay.CreateRelay(false);
            }
            else
            {
                NetworkManager.Singleton.StartServer();
            }

            EnableServerInterface();
        });

        clientButton.onClick.AddListener(() => {
            if (relay.UsingRelay())
            {
                // Support for delaying join for testing purposes
                try
                {
                    float delay = float.Parse(playerNameInput.text);
                    if (delay < 0) throw new FormatException();

                    Logger.Default.Log($"Delaying join for {delay} second(s)");
                    this.Invoke(() => {
                        relay.JoinRelay(joinCodeInput.text);
                    }, delay);
                }
                catch (FormatException)
                {
                    relay.JoinRelay(joinCodeInput.text);
                }
            }
            else
            {
                NetworkManager.Singleton.StartClient();
            }
            EnableClientInterface();
        });

        startButton.onClick.AddListener(() => {
            NetworkManager.Singleton.SceneManager.LoadScene("ArenaMain", LoadSceneMode.Single);
        });
    }

    private void EnableServerInterface()
    {
        GameState.Instance.SetLocalClientName(playerNameInput.text);

        hostButton.gameObject.SetActive(false);
        serverButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
        joinCodeInput.gameObject.SetActive(false);
        playerNameInput.gameObject.SetActive(false);

        if (relay.UsingRelay())
            serverCreatingMessage.SetActive(true);
        else
            EnableServerReadyInterface("");

    }

    private void EnableServerReadyInterface(string joinCode)
    {
        ClientNetwork.Instance.OnWaitingForClients += () => {
            startButton.interactable = false;
            waitingForClientsText.gameObject.SetActive(true);
        };

        ClientNetwork.Instance.OnClientsReady += () => {
            this.Invoke(() => { // Its too fast. I want to see the waiting for clients message at least a bit ¯\_(ツ)_/¯
                startButton.interactable = true;
                waitingForClientsText.gameObject.SetActive(false);
            }, 2f);
        };

        serverCreatingMessage.SetActive(false);
        startButton.gameObject.SetActive(true);
        serverClientsConnectedText.gameObject.SetActive(true);

        if (relay.UsingRelay())
        {
            joinCodeText.gameObject.SetActive(true);
            joinCodeText.text = joinCode;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
            serverClientsConnectedText.text = $"Players connected: {NetworkManager.Singleton.ConnectedClientsList.Count}";
        };
    }

    private void EnableClientInterface()
    {
        GameState.Instance.SetLocalClientName(playerNameInput.text);

        hostButton.gameObject.SetActive(false);
        serverButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
        joinCodeInput.gameObject.SetActive(false);

        clientConnectingMessage.SetActive(true);
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
            EnableClientConnectedInterface();
        };
    }

    private void EnableClientConnectedInterface()
    {
        clientConnectingMessage.SetActive(false);
        clientWaitMessage.SetActive(true);
    }
}
