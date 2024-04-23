using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameSetupPlayerList : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerTextPrefab;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private float initialYOffset = 0f;
    [SerializeField] private float playerGapBetween = 10f;

    private readonly Dictionary<ulong, TextMeshProUGUI> playerTexts = new();
    private float currentGap = 0;

    private void Start()
    {
        currentGap = initialYOffset;
    }

    public void AddPlayer(ulong clientId, string playerName)
    {
        TextMeshProUGUI playerText = Instantiate(playerTextPrefab, transform);
        playerText.text = playerName;

        playerText.transform.localPosition = new Vector3(0, currentGap, 0);
        currentGap -= playerGapBetween;

        playerTexts[clientId] = playerText;
        playerCountText.text = "Players: " + playerTexts.Count.ToString();
    }

    public void RemovePlayer(ulong clientId)
    {
        if (playerTexts.ContainsKey(clientId))
        {
            Destroy(playerTexts[clientId].gameObject);
            playerTexts.Remove(clientId);
            playerCountText.text = "Players: " + playerTexts.Count.ToString();

            currentGap = initialYOffset;
            foreach (var playerText in playerTexts.Values)
            {
                playerText.transform.localPosition = new Vector3(0, currentGap, 0);
                currentGap -= playerGapBetween;
            }
        }
    }
}
