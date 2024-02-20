using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EliminationFeedEntry : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI eliminatorName;
    [SerializeField] TextMeshProUGUI victimName;

    public void SetNames(string eliminator, string victim)
    {
        if (eliminatorName == null)
            eliminatorName.gameObject.SetActive(false);
        else
            eliminatorName.text = eliminator;

        victimName.text = victim;
    }
}
