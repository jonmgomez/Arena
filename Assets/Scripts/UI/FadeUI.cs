using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private Coroutine currentFadeCoroutine;

    public void FadeIn(float duration, float delay = 0f)
    {
        Fade(true, duration, delay);
    }

    public void FadeOut(float duration, float delay = 0f)
    {
        Fade(false, duration, delay);
    }

    private void Fade(bool fadeIn, float duration, float delay = 0f)
    {
        if (duration == 0)
        {
            canvasGroup.alpha = fadeIn ? 1f : 0f;
            return;
        }

        if (delay > 0)
            currentFadeCoroutine = this.RestartCoroutine(Delay(fadeIn, duration), currentFadeCoroutine);
        else if (fadeIn)
            currentFadeCoroutine = this.RestartCoroutine(FadeInCoroutine(duration), currentFadeCoroutine);
        else
            currentFadeCoroutine = this.RestartCoroutine(FadeOutCoroutine(duration), currentFadeCoroutine);
    }

    private IEnumerator Delay(bool fadeIn, float duration)
    {
        yield return new WaitForSeconds(duration);
        Fade(fadeIn, duration);
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
