using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeForAllGameMode : GameModeController
{
    [SerializeField] private int scoreToWin = 30;

    void Update()
    {
        CalculateTimer();
    }

    public override void CheckGameScoresForWin()
    {
        List<Player> players = GameState.Instance.GetPlayers();
        foreach (Player player in players)
        {
            if (player.GetPlayerScore().GetScore(ScoreType.Elimination) >= scoreToWin)
            {
                logger.Log(Utility.PlayerNameToString(player) + " has won the game!");

                EndGame(player.GetName());
                return;
            }
        }
    }

    public override void CheckWinnerOnTimesUp()
    {
        List<Player> players = GameState.Instance.GetPlayers();
        List<Player> highestScorers = new();
        int highestScore = 0;

        foreach (Player player in players)
        {
            int score = player.GetPlayerScore().GetScore(ScoreType.Elimination);
            if (score > highestScore)
            {
                highestScorers.Clear();
                highestScorers.Add(player);
                highestScore = score;
            }
            else if (score == highestScore)
            {
                highestScorers.Add(player);
            }
        }

        if (highestScorers.Count == 1)
        {
            logger.Log(Utility.PlayerNameToString(highestScorers[0]) + " has won the game!");
            EndGame(highestScorers[0].GetName());
        }
        else
        {
            logger.Log("It's a draw!");
            EndGame(null);
        }
    }

    public override string GetGameModeName() => "Free For All";
    public override string GetWinConditionDescription() => $"First to {scoreToWin} eliminations wins!";
}
