using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreCard : MonoBehaviour
{
    [SerializeField] private Color isThisPlayerColor;
    [SerializeField] private Image cardBackground;
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private TextMeshProUGUI eliminations;
    [SerializeField] private TextMeshProUGUI deaths;
    [SerializeField] private TextMeshProUGUI assists;

    public void SetName(string name, bool isThisPlayer)
    {
        // Set the color of the player name based on whether it is local player
        if (isThisPlayer)
            cardBackground.color = isThisPlayerColor;

        playerName.text = name;
    }

    public void SetScore(int eliminations, int deaths, int assists)
    {
        this.eliminations.text = eliminations.ToString();
        this.deaths.text = deaths.ToString();
        this.assists.text = assists.ToString();
    }
}
