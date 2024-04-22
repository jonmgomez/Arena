using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameSetupScreen : MonoBehaviour
{
    private const string JOIN_CODE_TEXT = "Lobby Code: ";

    [SerializeField] private GameSetupData gameSetupData;
    [SerializeField] private GameSetupModeSettings gameModeSettings;
    [SerializeField] private GameSetupPlayerList playerList;

    [SerializeField] private TMP_Dropdown gameModeSelector;
    [SerializeField] private TextMeshProUGUI joinCodeText;

    private RelayManager relay;

    void Start()
    {
        relay = FindObjectOfType<RelayManager>(); if (relay == null) return;
        string joinCode = relay.GetJoinCode();
        joinCodeText.text = JOIN_CODE_TEXT + joinCode;

        gameModeSelector.onValueChanged.AddListener(OnGameModeChanged);

        List<ClientData> clients = GameState.Instance.GetConnectedClients();
        foreach (ClientData client in clients)
        {
            Debug.Log("Adding player at start: " + client.clientName);
            playerList.AddPlayer(client.clientName);
        }

        GameState.Instance.ClientReady += (ulong clientId) =>
        {
            ClientData client = GameState.Instance.GetClientData(clientId);

            if (client.clientName == null)
            {
                Debug.Log("Client name is null");
                return;
            }

            playerList.AddPlayer(client.clientName);
        };
    }

    private void OnGameModeChanged(int value)
    {
        GameMode gameMode = (GameMode) value;
        gameModeSettings.SetGameMode(gameMode);
        gameSetupData.GameMode = gameMode;
    }

    public void CopyJoinCode()
    {
        GUIUtility.systemCopyBuffer = relay.GetJoinCode();
    }
}
