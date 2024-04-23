using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private RelayManager relay;
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button serverButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI nextButtonText;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TextMeshProUGUI waitMessage;

    private bool creatingLobby = false;

    void Start()
    {
        createGameButton.onClick.AddListener(CreateGameStart);
        serverButton.onClick.AddListener(StartServer);
        joinGameButton.onClick.AddListener(JoinGameStart);
        nextButton.onClick.AddListener(NextAction);
        exitButton.onClick.AddListener(QuitApplication);

        createGameButton.gameObject.SetActive(true);
        joinGameButton.gameObject.SetActive(true);
        exitButton.gameObject.SetActive(true);

        nextButton.gameObject.SetActive(false);
        joinCodeInput.gameObject.SetActive(false);
        playerNameInput.gameObject.SetActive(false);
        waitMessage.gameObject.SetActive(false);
    }

    private void StartHost()
    {
        if (relay.UsingRelay())
        {
            relay.OnJoinCodeGenerated += (joinCode) => {
                SceneLoader.LoadSceneNetworked(Scene.GameSelect);
            };
            relay.CreateRelay();
        }
        else
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    private void StartServer()
    {
        GameState.Instance.SetLocalClientName("Server");

        if (relay.UsingRelay())
        {
            relay.OnJoinCodeGenerated += (joinCode) => {
                SceneLoader.LoadSceneNetworked(Scene.GameSelect);
            };
            relay.CreateRelay(false);
        }
        else
        {
            NetworkManager.Singleton.StartServer();
        }
    }

    private void StartClient()
    {
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
    }

    private void NextAction()
    {
        GameState.Instance.SetLocalClientName(playerNameInput.text);
        if (creatingLobby)
        {
            StartHost();
            EnableWaitMessage("Creating");
        }
        else
        {
            StartClient();
            EnableWaitMessage("Finding Game");
        }
    }

    private void CreateGameStart()
    {
        creatingLobby = true;
        nextButtonText.text = "Create";

        EnableAskForName();
    }

    private void JoinGameStart()
    {
        creatingLobby = false;
        nextButtonText.text = "Join";

        EnableAskForName();
        EnableAskForJoinCode();
    }

    private void EnableAskForName()
    {
        createGameButton.gameObject.SetActive(false);
        joinGameButton.gameObject.SetActive(false);
        serverButton.gameObject.SetActive(false);

        playerNameInput.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(true);
    }

    private void EnableAskForJoinCode()
    {
        joinCodeInput.gameObject.SetActive(true);
        joinCodeInput.onValueChanged.AddListener((joinCode) =>
        {
            if (joinCode.Length >= 5)
                nextButton.interactable = true;
            else
                nextButton.interactable = false;
        });

        nextButton.interactable = false;
    }

    private void EnableWaitMessage(string message)
    {
        playerNameInput.gameObject.SetActive(false);
        joinCodeInput.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);

        waitMessage.text = message;
        waitMessage.gameObject.SetActive(true);
    }

    private void QuitApplication()
    {
        Application.Quit();
    }
}
