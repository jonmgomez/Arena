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
        public readonly int VariableCode;
        private T value;
        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                dataContainer.VariableChanged(VariableCode, Convert.ToDouble(value));
                OnValueChanged?.Invoke(value);
            }
        }

        public event Action<T> OnValueChanged;

        public GameSetupVariable(GameSetupData gameSetupData)
        {
            dataContainer = gameSetupData;
            VariableCode = variableCodeCounter++;
        }
    }

    public GameSetupVariable<Scene> Map = null;
    public GameSetupVariable<GameMode> GameMode = null;
    public GameSetupVariable<float> TimeLimit = null;
    public GameSetupVariable<int> ScoreLimit = null;

    void Awake()
    {
        Map = new GameSetupVariable<Scene>(this);
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
                Map.Value = (Scene)value;
                break;
            case 1:
                GameMode.Value = (GameMode)value;
                break;
            case 2:
                TimeLimit.Value = (float)value;
                break;
            case 3:
                ScoreLimit.Value = (int)value;
                break;
        }
    }

    public void SyncCurrentData()
    {
        if (IsClient)
        {
            RequestGameSettingsServerRpc(Net.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGameSettingsServerRpc(ulong clientId)
    {
        UpdateVariableClientRpc(Map.VariableCode, (double)Map.Value, Utility.SendToOneClient(clientId));
        UpdateVariableClientRpc(GameMode.VariableCode, (double)GameMode.Value, Utility.SendToOneClient(clientId));
        UpdateVariableClientRpc(TimeLimit.VariableCode, (double)TimeLimit.Value, Utility.SendToOneClient(clientId));
        UpdateVariableClientRpc(ScoreLimit.VariableCode, (double)ScoreLimit.Value, Utility.SendToOneClient(clientId));
    }
}
