using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private TextMeshProUGUI eliminations;
    [SerializeField] private TextMeshProUGUI deaths;
    [SerializeField] private TextMeshProUGUI assists;

    public void SetName(string name)
    {
        playerName.text = name;
    }

    public void SetScore(int eliminations, int deaths, int assists)
    {
        this.eliminations.text = eliminations.ToString();
        this.deaths.text = deaths.ToString();
        this.assists.text = assists.ToString();
    }
}
