using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameModeController : MonoBehaviour
{
    protected readonly Logger logger = new("GAMEMODE");

    private float startingTimeLeft = 300f;
    private float timeLeft = 0f;
    private int timeLeftAsInt = 0; // For display purposes
    private bool isGameActive = false;

    private ScoreBoard scoreBoard;

    /// <summary>
    /// Check the game scores to see if a player or team has won
    /// </summary>
    public abstract void CheckGameScoresForWin();

    /// <summary>
    /// Check for the winner when the time runs out (based on highest score)
    /// </summary>
    public abstract void CheckWinnerOnTimesUp();

    /// <summary>
    /// Get the name of the game mode
    /// </summary>
    public abstract string GetGameModeName();

    public void CalculateTimer()
    {
        if (isGameActive)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0)
            {
                logger.Log("Game Over");
                isGameActive = false;
                CheckWinnerOnTimesUp();
            }

            if ((int)timeLeft != timeLeftAsInt)
            {
                timeLeftAsInt = (int)timeLeft;
                if (scoreBoard != null)
                {
                    scoreBoard.SetGameTimeLeft(timeLeftAsInt);
                }
            }
        }
    }

    public void StartGame()
    {
        logger.Log("Game Started");
        isGameActive = true;
        timeLeft = startingTimeLeft;
        timeLeftAsInt = (int)timeLeft;

        EnableLocalPlayerControls(true);

        scoreBoard = FindObjectOfType<ScoreBoard>();
    }

    public void EndGame(string winnerName) // Singular player or team
    {
        logger.Log("Game Ended");
        isGameActive = false;

        EnableLocalPlayerControls(false);

        FindObjectOfType<GameOverScreen>().ShowGameOverScreen(winnerName);

        InGameController.Instance.OnGameEnded();
    }

    private void EnableLocalPlayerControls(bool enableControls)
    {
        Player localPlayer = GameState.Instance.GetLocalPlayer();
        if (localPlayer != null)
        {
            localPlayer.SetEnableControls(enableControls);
        }
    }

    public float GetTimeLeft() => timeLeft;
}
