using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreBoard : MonoBehaviour
{
    [SerializeField] private GameObject scoreBoard;
    [SerializeField] private ScoreCard playerScorePrefab;
    [SerializeField] private Vector3 playerScoreOffset;
    [SerializeField] private float playerScoreSpacing;
    [SerializeField] private Transform playerScoreParent;

    private Dictionary<PlayerScore, ScoreCard> playerScores = new();
    private float currentYOffset = 0;

    void Start()
    {
        scoreBoard.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            scoreBoard.SetActive(true);
            UpdateScoreBoard();
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            scoreBoard.SetActive(false);
        }
    }

    void UpdateScoreBoard()
    {
        foreach (var playerScorePair in playerScores)
        {
            PlayerScore score = playerScorePair.Key;
            ScoreCard scoreCard = playerScorePair.Value;
            scoreCard.SetScore(score.GetScore(ScoreType.Elimination),
                               score.GetScore(ScoreType.Death),
                               score.GetScore(ScoreType.Assist));
        }
    }

    public void CreatePlayerScoreCards(List<Player> players)
    {
        Debug.Log(players.Count);
        foreach (var player in players)
        {
            Debug.Log(player);
            Debug.Log(player.GetPlayerScore());
            if (playerScores.ContainsKey(player.GetPlayerScore()))
                continue;

            ScoreCard scoreCard = Instantiate(playerScorePrefab, playerScoreParent);
            scoreCard.SetName(player.GetName());

            scoreCard.transform.localPosition = new Vector3(playerScoreOffset.x, playerScoreOffset.y + currentYOffset, playerScoreOffset.z);
            currentYOffset -= playerScoreSpacing;

            playerScores.Add(player.GetPlayerScore(), scoreCard);
        }
    }
}
