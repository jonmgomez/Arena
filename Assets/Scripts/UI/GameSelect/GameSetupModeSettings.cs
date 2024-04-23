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
    private bool editable = true;

    void Start()
    {
        gameSetupData =  FindObjectOfType<GameSetupData>();

        SetTimeLimitValue(DEFAULT_TIME_LIMIT);
        SetScoreLimitValue(DEFAULT_SCORE_LIMIT);

        timeLimitTextArea.onEndEdit.AddListener(SetTimeLimit);
        gameSetupData.TimeLimit.OnValueChanged += (value) => { timeLimitTextArea.text = value.ToString(); };
        scoreLimitTextArea.onEndEdit.AddListener(SetScoreLimit);
        gameSetupData.ScoreLimit.OnValueChanged += (value) => { scoreLimitTextArea.text = value.ToString(); };
    }

    public void SetGameMode(GameMode gameMode)
    {
        gameSetupData.GameMode.Value = gameMode;
    }

    public void SetTimeLimitValue(int value)
    {
        gameSetupData.TimeLimit.Value = value;
        timeLimitTextArea.text = value.ToString();
    }

    private void SetScoreLimitValue(int value)
    {
        gameSetupData.ScoreLimit.Value = value;
        scoreLimitTextArea.text = value.ToString();
    }

    public void SetTimeLimit(string time)
    {
        if (time == "")
        {
            SetTimeLimitValue(DEFAULT_TIME_LIMIT);
            return;
        }

        int timeLimit = System.Convert.ToInt32(time);
        if (timeLimit < MINIMUM_TIME_LIMIT)
        {
            SetTimeLimitValue(MINIMUM_TIME_LIMIT);
            return;
        }

        SetTimeLimitValue(timeLimit);
    }

    public void SetScoreLimit(string score)
    {
        if (score == "")
        {
            SetScoreLimitValue(DEFAULT_SCORE_LIMIT);
            return;
        }

        int scoreLimit = System.Convert.ToInt32(score);
        if (scoreLimit < MINIMUM_SCORE_LIMIT)
        {
            SetScoreLimitValue(MINIMUM_SCORE_LIMIT);
            return;
        }

        SetScoreLimitValue(scoreLimit);
    }

    public void SetEditable(bool value)
    {
        editable = value;
        timeLimitTextArea.interactable = value;
        scoreLimitTextArea.interactable = value;
    }
}
