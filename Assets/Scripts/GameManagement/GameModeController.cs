using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameModeController : MonoBehaviour
{
    private float timeLeft = 300.0f;
    private bool isGameActive = false;

    public abstract void CheckGameScores();

    public void CalculateTimer()
    {
        if (isGameActive)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0)
            {
                Logger.Default.Log("Game Over");
                isGameActive = false;
                CheckGameScores();
            }
        }
    }

    public void StartGame()
    {
        Logger.Default.Log("Game Started");
        isGameActive = true;
    }
}
