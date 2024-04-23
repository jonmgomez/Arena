using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameSetupData : NetworkBehaviour
{
    private static int variableCodeCounter = 0;

    /// <summary>
    /// This is a game variable that can be changed by the server and will be updated on the clients.
    /// Using this over SyncVars as it allows for more flexibility and control.
    /// </summary>
    public class GameSetupVariable<T>
    {
        private readonly GameSetupData dataContainer;
        public readonly int variableCode;
        private T value;
        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                dataContainer.VariableChanged(variableCode, Convert.ToDouble(value));
                OnValueChanged?.Invoke(value);
            }
        }

        public event Action<T> OnValueChanged;

        public GameSetupVariable(GameSetupData gameSetupData)
        {
            dataContainer = gameSetupData;
            variableCode = variableCodeCounter++;
        }
    }

    public static GameSetupData Instance { get; private set; }

    public GameSetupVariable<GameMode> GameMode = null; // 0
    public GameSetupVariable<float> TimeLimit = null; // 1
    public GameSetupVariable<int> ScoreLimit = null; // 2

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        GameMode = new GameSetupVariable<GameMode>(this);
        TimeLimit = new GameSetupVariable<float>(this);
        ScoreLimit = new GameSetupVariable<int>(this);
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void VariableChanged(int variableCode, double value)
    {
        if (IsServer)
        {
            UpdateVariableClientRpc(variableCode, value);
        }
    }

    [ClientRpc]
    private void UpdateVariableClientRpc(int variableCode, double value, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;

        switch (variableCode)
        {
            case 0:
                GameMode.Value = (GameMode)value;
                break;
            case 1:
                TimeLimit.Value = (float)value;
                break;
            case 2:
                ScoreLimit.Value = (int)value;
                break;
        }
    }

    public void SyncData()
    {
        if (IsClient)
        {
            RequestGameSettingsServerRpc(Net.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGameSettingsServerRpc(ulong clientId)
    {
        UpdateVariableClientRpc(0, (double)GameMode.Value, Utility.SendToOneClient(clientId));
        UpdateVariableClientRpc(1, (double)TimeLimit.Value, Utility.SendToOneClient(clientId));
        UpdateVariableClientRpc(2, (double)ScoreLimit.Value, Utility.SendToOneClient(clientId));
    }
}
