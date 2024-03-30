using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EliminationFeedEntry : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI eliminatorName;
    [SerializeField] TextMeshProUGUI victimName;
    [SerializeField] FadeUI fade;

    public void SetNames(string eliminator, string victim)
    {
        if (eliminatorName == null)
            eliminatorName.gameObject.SetActive(false);
        else
            eliminatorName.text = eliminator;

        victimName.text = victim;
    }

    public void FadeIn(float duration)
    {
        fade.FadeIn(duration);
    }

    public void FadeOut(float duration)
    {
        fade.FadeOut(duration);
    }
}
