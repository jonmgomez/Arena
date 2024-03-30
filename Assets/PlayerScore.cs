using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ScoreType
{
    Elimination,
    Death,
    Assist
}

public class PlayerScore : MonoBehaviour
{
    private readonly Logger logger = new("PLYRSCORE");

    private int eliminations = 0;
    private int deaths       = 0;
    private int assists      = 0;

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
                break;
        }
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
