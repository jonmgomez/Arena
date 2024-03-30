using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    public void FadeIn(float duration)
    {
        StartCoroutine(FadeInCoroutine(duration));
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }

    private IEnumerator FadeInCoroutine(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            yield return null;
        }
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            yield return null;
        }
    }
}
