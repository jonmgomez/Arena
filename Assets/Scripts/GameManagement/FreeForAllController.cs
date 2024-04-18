using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeForAllController : GameModeController
{
    private int scoreToWin = 2;

    void Update()
    {
        CalculateTimer();
    }

    public override void CheckGameScores()
    {
        Logger.Default.Log("Checking Game Scores");

        List<Player> players = GameState.Instance.GetPlayers();
        foreach (Player player in players)
        {
            if (player.GetPlayerScore().GetScore(ScoreType.Elimination) >= scoreToWin)
            {
                Logger.Default.Log(Utility.PlayerNameToString(player) + " has won the game!");

                EndGame();
                return;
            }
        }
    }
}
