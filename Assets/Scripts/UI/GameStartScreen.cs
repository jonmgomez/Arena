using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameStartScreen : MonoBehaviour
{
    [SerializeField] private GameObject gameStartScreen;
    [SerializeField] private TextMeshProUGUI remainingTimeText;

    void Start()
    {
        gameStartScreen.SetActive(false);
    }

    public void ShowGameStartScreen(float timeLeft)
    {
        gameStartScreen.SetActive(true);
        StartCoroutine(GameTimerCountDown(timeLeft));
    }

    private IEnumerator GameTimerCountDown(float timeLeft)
    {
        while (timeLeft > 0)
        {
            remainingTimeText.text = ((int) timeLeft).ToString();

            yield return new WaitForSeconds(1);

            timeLeft--;
        }

        gameStartScreen.SetActive(false);
    }
}
