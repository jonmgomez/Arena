using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBoard : UIMenu
{
    private class ScoreEntry
    {
        public readonly ulong ClientId;
        public readonly PlayerScore PlayerScore;
        public readonly ScoreCard ScoreCard;

        public ScoreEntry(ulong clientId, PlayerScore playerScore, ScoreCard scoreCard)
        {
            ClientId = clientId;
            PlayerScore = playerScore;
            ScoreCard = scoreCard;
        }
    }

    [SerializeField] private GameObject scoreBoard;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private TextMeshProUGUI timeLeftText;
    [SerializeField] private TextMeshProUGUI team1ScoreText;
    [SerializeField] private TextMeshProUGUI team2ScoreText;
    [SerializeField] private ScoreCard playerScorePrefab;
    [SerializeField] private Vector3 playerScoreOffset;
    [SerializeField] private float playerScoreSpacing;
    [SerializeField] private Transform playerScoreParent;

    private readonly List<ScoreEntry> scoreEntries = new();

    void Start()
    {
        scoreBoard.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TrySetMenuEnabled();
        }
    }

    public override void SetMenuEnabled(bool enabled)
    {
        scoreBoard.SetActive(enabled);
    }

    public void ForceScoreBoardEnabled()
    {
        MenuManager menuManager = MenuManager.Instance;
        menuManager.ForceMenuEnabled(this);
    }

    public void UpdateScoreBoard()
    {
        foreach (var entry in scoreEntries)
        {
            PlayerScore score = entry.PlayerScore;
            ScoreCard scoreCard = entry.ScoreCard;
            scoreCard.SetScore(score.GetScore(ScoreType.Elimination),
                               score.GetScore(ScoreType.Death),
                               score.GetScore(ScoreType.Assist));
        }

        scoreEntries.Sort((a, b) => b.PlayerScore.GetScore(ScoreType.Elimination) - a.PlayerScore.GetScore(ScoreType.Elimination));

        float currentYOffset = 0;
        foreach (var entry in scoreEntries)
        {
            entry.ScoreCard.transform.localPosition = new Vector3(playerScoreOffset.x, playerScoreOffset.y + currentYOffset, playerScoreOffset.z);
            currentYOffset -= playerScoreSpacing;
        }
    }

    public void CreatePlayerScoreCard(Player player)
    {
        Debug.Assert(scoreEntries.All(x => x.PlayerScore != player.GetPlayerScore()), "Player score card already exists");

        ScoreCard scoreCard = Instantiate(playerScorePrefab, playerScoreParent);

        bool isLocalPlayer = Net.IsLocalClient(player.OwnerClientId);
        scoreCard.SetName(player.GetName(), isLocalPlayer);
        scoreEntries.Add(new ScoreEntry(player.OwnerClientId, player.GetPlayerScore(), scoreCard));

        UpdateScoreBoard();
    }

    public void RemovePlayerScoreCard(ulong clientId)
    {
        ScoreEntry entry = scoreEntries.FirstOrDefault(x => x.ClientId == clientId);
        if (entry != null)
        {
            scoreEntries.Remove(entry);
            Destroy(entry.ScoreCard.gameObject);
            UpdateScoreBoard();
        }
    }

    public void SetGameMode(GameModeController gameMode)
    {
        gameModeText.text = gameMode.GetGameModeName();
        timeLeftText.text = ((int) gameMode.GetTimeLeft()).ToString();

        if (gameMode is FreeForAllController)
        {
            team1ScoreText.gameObject.SetActive(false);
            team2ScoreText.gameObject.SetActive(false);
        }
    }

    public void SetGameTimeLeft(float timeLeft)
    {
        SetGameTimeLeft((int) timeLeft);
    }


    public void SetGameTimeLeft(int timeLeft)
    {
        timeLeftText.text = timeLeft.ToString();
    }
}
