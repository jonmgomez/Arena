using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSetupScreen : MonoBehaviour
{
    private const string JOIN_CODE_TEXT = "Lobby Code: ";

    [SerializeField] private GameSetupModeSettings gameModeSettings;
    [SerializeField] private GameSetupPlayerList playerList;

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject gameSetupScreen;

    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Dropdown gameModeSelector;
    [SerializeField] private TMP_Dropdown mapSelector;
    [SerializeField] private TextMeshProUGUI joinCodeText;

    private GameSetupData gameSetupData;
    private RelayManager relay;
    private bool loading = false;

    void Start()
    {
        relay = FindObjectOfType<RelayManager>(); if (relay == null) return;
        string joinCode = relay.GetJoinCode();
        joinCodeText.text = JOIN_CODE_TEXT + joinCode;

        gameModeSelector.onValueChanged.AddListener(OnGameModeChanged);
        gameSetupData = FindObjectOfType<GameSetupData>();
        gameSetupData.GameMode.OnValueChanged += (value) => gameModeSelector.value = (int) value;

        GameState.Instance.ClientReady += (ulong clientId) =>
        {
            if (loading)
            {
                if (Net.IsLocalClient(clientId))
                {
                    ShowGameSetup();
                }
            }
            else
            {
                ClientData client = GameState.Instance.GetClientData(clientId);
                AddPlayerName(client);
            }
        };

        ClientNetwork.Instance.OnClientDisconnected += (ulong clientId) =>
        {
            playerList.RemovePlayer(clientId);
        };

        // Clients need to wait for the server to notify them that they have properly synced first.
        // Known through invoking the ClientReady event with the local client's ID.
        if (Net.IsClientOnly)
        {
            ShowLoading();
            gameSetupData.SyncCurrentData(); // Upon joining, sync to the server's current data.
            return;
        }

        ShowGameSetup();
    }

    private void ShowLoading()
    {
        loading = true;
        loadingScreen.SetActive(true);
        gameSetupScreen.SetActive(false);
    }

    private void ShowGameSetup()
    {
        loading = false;
        loadingScreen.SetActive(false);
        gameSetupScreen.SetActive(true);

        List<ClientData> clients = GameState.Instance.GetConnectedClients();
        clients.ForEach(AddPlayerName);

        if (Net.IsServer)
        {
            gameModeSelector.interactable = true;
            mapSelector.interactable = true;
            gameModeSettings.SetEditable(true);

            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.SceneManager.LoadScene("ArenaMain", LoadSceneMode.Single);
            });
        }
        else
        {
            gameModeSelector.interactable = false;
            mapSelector.interactable = false;
            gameModeSettings.SetEditable(false);

            startButton.gameObject.SetActive(false);
        }
    }

    private void AddPlayerName(ClientData client)
    {
        if (client.clientName == null)
        {
            Logger.Default.Log("Client name is null");
            return;
        }
        playerList.AddPlayer(client.clientId, client.clientName);
    }

    private void OnGameModeChanged(int value)
    {
        GameMode gameMode = (GameMode) value;
        gameModeSettings.SetGameMode(gameMode);
    }

    public void CopyJoinCode()
    {
        GUIUtility.systemCopyBuffer = relay.GetJoinCode();
    }
}
