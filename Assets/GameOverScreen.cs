using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen;

    [SerializeField] private float timeBeforeScoreBoard = 3f;

    void Start()
    {
        gameOverScreen.SetActive(false);
    }

    public void ShowGameOverScreen()
    {
        gameOverScreen.SetActive(true);

        StartCoroutine(ShowScoreBoard());
    }

    private IEnumerator ShowScoreBoard()
    {
        yield return new WaitForSeconds(timeBeforeScoreBoard);

        FindObjectOfType<ScoreBoard>().ForceScoreBoardEnabled();
        gameOverScreen.SetActive(false);
    }
}
