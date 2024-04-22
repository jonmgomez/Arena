using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameModeController : MonoBehaviour
{
    public static float DEFAULT_MAX_TIME = 300f;
    protected readonly Logger logger = new("GAMEMODE");

    [SerializeField] private float startingTimeLeft = 300f;

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

    /// <summary>
    /// Get the description of the win condition
    /// </summary>
    public abstract string GetWinConditionDescription();

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

    public void SetMaxTime(float maxTime) => startingTimeLeft = maxTime;
    public float GetMaxTime() => startingTimeLeft;

    public void SetTimeLeft(float timeLeft) => this.timeLeft = timeLeft;
    public float GetTimeLeft() => timeLeft;
}
