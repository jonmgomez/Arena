using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameSetupModeSettings : MonoBehaviour
{
    private const int DEFAULT_TIME_LIMIT = 300;
    private const int MINIMUM_TIME_LIMIT = 10;
    private const int DEFAULT_SCORE_LIMIT = 30;
    private const int MINIMUM_SCORE_LIMIT = 1;

    [SerializeField] private TMP_InputField timeLimitTextArea;
    [SerializeField] private TMP_InputField scoreLimitTextArea;

    private GameSetupData gameSetupData;

    void Start()
    {
        gameSetupData = FindObjectOfType<GameSetupData>();

        timeLimitTextArea.onEndEdit.AddListener(SetTimeLimit);
        scoreLimitTextArea.onEndEdit.AddListener(SetScoreLimit);
    }

    public void SetGameMode(GameMode gameMode)
    {
        gameSetupData.GameMode = gameMode;
    }

    public void SetTimeLimit(string time)
    {
        if (time == "")
        {
            gameSetupData.TimeLimit = DEFAULT_TIME_LIMIT;
            timeLimitTextArea.text = DEFAULT_TIME_LIMIT.ToString();
            return;
        }

        int timeLimit = System.Convert.ToInt32(time);
        if (timeLimit < MINIMUM_TIME_LIMIT)
        {
            gameSetupData.TimeLimit = MINIMUM_TIME_LIMIT;
            timeLimitTextArea.text = MINIMUM_TIME_LIMIT.ToString();
            return;
        }

        gameSetupData.TimeLimit = timeLimit;
        timeLimitTextArea.text = timeLimit.ToString();
    }

    public void SetScoreLimit(string score)
    {
        if (score == "")
        {
            gameSetupData.ScoreLimit = DEFAULT_SCORE_LIMIT;
            scoreLimitTextArea.text = DEFAULT_SCORE_LIMIT.ToString();
            return;
        }

        int scoreLimit = System.Convert.ToInt32(score);
        if (scoreLimit < MINIMUM_SCORE_LIMIT)
        {
            gameSetupData.ScoreLimit = MINIMUM_SCORE_LIMIT;
            scoreLimitTextArea.text = MINIMUM_SCORE_LIMIT.ToString();
            return;
        }

        gameSetupData.ScoreLimit = scoreLimit;
        scoreLimitTextArea.text = scoreLimit.ToString();
    }
}
