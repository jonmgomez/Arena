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
    [SerializeField] private Button exitButton;
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

        mapSelector.onValueChanged.AddListener(OnMapChanged);

        gameModeSelector.onValueChanged.AddListener(OnGameModeChanged);
        gameSetupData = FindObjectOfType<GameSetupData>();
        gameSetupData.GameMode.OnValueChanged += (value) => gameModeSelector.value = (int) value;
        gameSetupData.Map.OnValueChanged += (value) => mapSelector.value = (int) value;

        GameState.Instance.ClientReady += OnClientReady;
        ClientNetwork.Instance.OnClientDisconnected += OnClientDisconnect;
        ClientNetwork.Instance.OnSelfDisconnect += OnSelfDisconnect;

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

    private void OnClientReady(ulong clientId)
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
    }

    private void OnClientDisconnect(ulong clientId)
    {
        playerList.RemovePlayer(clientId);
    }

    private void OnSelfDisconnect(bool wasServer)
    {
        ExitBackToMainMenu(false);
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

        exitButton.onClick.AddListener(() => ExitBackToMainMenu());
        if (Net.IsServer)
        {
            gameModeSelector.interactable = true;
            mapSelector.interactable = true;
            gameModeSettings.SetEditable(true);

            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(() =>
            {
                SceneLoader.LoadSceneNetworked(gameSetupData.Map.Value);
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

    private void OnMapChanged(int value)
    {
        Scene scene = (Scene) value;
        gameSetupData.Map.Value = scene;
    }

    public void CopyJoinCode()
    {
        GUIUtility.systemCopyBuffer = relay.GetJoinCode();
    }

    public void ExitBackToMainMenu(bool disconnect = true)
    {
        if (disconnect) // In case we get disconnected unintentionally
        {
            NetworkManager.Singleton.Shutdown();
        }

        GameState.Instance.ClientReady -= OnClientReady;
        ClientNetwork.Instance.OnClientDisconnected -= OnClientDisconnect;
        ClientNetwork.Instance.OnSelfDisconnect -= OnSelfDisconnect;
        Destroy(gameSetupData.gameObject);

        SceneLoader.LoadScene(Scene.MainMenu);
    }
}
