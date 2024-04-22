using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameSetupPlayerList : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerTextPrefab;
    [SerializeField] private float initialYOffset = 0f;
    [SerializeField] private float playerGapBetween = 10f;

    private float currentGap = 0;

    private void Start()
    {
        currentGap = initialYOffset;
    }

    public void AddPlayer(string playerName)
    {
        TextMeshProUGUI playerText = Instantiate(playerTextPrefab, transform);
        playerText.text = playerName;

        playerText.transform.localPosition = new Vector3(0, currentGap, 0);
        currentGap -= playerGapBetween;
    }
}
