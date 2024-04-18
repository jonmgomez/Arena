using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TextMeshProUGUI winnerNameText;
    [SerializeField] private float timeBeforeScoreBoard = 3f;

    void Start()
    {
        gameOverScreen.SetActive(false);
    }

    public void ShowGameOverScreen(string playerName)
    {
        gameOverScreen.SetActive(true);
        winnerNameText.text = playerName + " Won";

        StartCoroutine(ShowScoreBoard());
    }

    private IEnumerator ShowScoreBoard()
    {
        MenuManager menuManager = MenuManager.Instance;
        menuManager.ForceMenuEnabled(null);

        yield return new WaitForSeconds(timeBeforeScoreBoard);

        FindObjectOfType<ScoreBoard>().ForceScoreBoardEnabled();
        gameOverScreen.SetActive(false);
    }
}
