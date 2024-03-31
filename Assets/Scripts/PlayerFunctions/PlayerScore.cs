using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public enum ScoreType
{
    Elimination,
    Death,
    Assist
}

public struct NetworkedPlayerScore : INetworkSerializable
{
    public ulong clientId;
    public int eliminations;
    public int deaths;
    public int assists;

    public NetworkedPlayerScore(ulong clientId, int eliminations, int deaths, int assists)
    {
        this.clientId = clientId;
        this.eliminations = eliminations;
        this.deaths = deaths;
        this.assists = assists;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref eliminations);
        serializer.SerializeValue(ref deaths);
        serializer.SerializeValue(ref assists);
    }
}

public class PlayerScore : NetworkBehaviour
{
    private readonly Logger logger = new("PLYRSCORE");

    private int eliminations = 0;
    private int deaths       = 0;
    private int assists      = 0;

    public void SyncScore()
    {
        if (!IsServer)
            RequestScoresServerRpc(Net.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestScoresServerRpc(ulong clientId)
    {
        NetworkedPlayerScore score = new NetworkedPlayerScore(OwnerClientId, eliminations, deaths, assists);
        SyncScoresClientRpc(score, Utility.SendToOneClient(clientId));
    }

    [ClientRpc]
    private void SyncScoresClientRpc(NetworkedPlayerScore score, ClientRpcParams clientRpcParams = default)
    {
        eliminations += score.eliminations;
        deaths += score.deaths;
        assists += score.assists;

        InGameController.Instance.UpdateScoreBoard();
    }

    public void IncreaseScore(ScoreType scoreType)
    {
        switch (scoreType)
        {
            case ScoreType.Elimination:
                eliminations++;
                break;
            case ScoreType.Death:
                deaths++;
                break;
            case ScoreType.Assist:
                assists++;
                break;
            default:
                logger.LogError("Score type not found");
                return;
        }

        InGameController.Instance.UpdateScoreBoard();
    }

    public int GetScore(ScoreType scoreType)
    {
        switch (scoreType)
        {
            case ScoreType.Elimination:
                return eliminations;
            case ScoreType.Death:
                return deaths;
            case ScoreType.Assist:
                return assists;
            default:
                logger.LogError("Score type not found");
                return -1;
        }
    }
}
