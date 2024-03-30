using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreBoard : MonoBehaviour
{
    [SerializeField] GameObject scoreBoard;
    [SerializeField] GameObject playerScorePrefab;
    [SerializeField] Transform playerScoreParent;

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
        foreach (Transform child in playerScoreParent)
        {
            Destroy(child.gameObject);
        }

        // foreach (var player in GameState.Instance.Players)
        // {
        //     GameObject playerScore = Instantiate(playerScorePrefab, playerScoreParent);
        //     playerScore.GetComponent<PlayerScore>().SetPlayer(player);
        // }
    }
}
