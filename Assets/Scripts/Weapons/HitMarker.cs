using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitMarker : MonoBehaviour
{
    [SerializeField] private Color headHitColor;
    [SerializeField] private float headHitWidth = 0.1f;
    [SerializeField] private Image[] hitMarkerImages;
    [SerializeField] private float fadeOutTime = 0.25f;
    [SerializeField] private float fadeOutDelay = 0.1f;

    private FadeUI fadeUI;

    private float originalWidth;

    void Start()
    {
        originalWidth = hitMarkerImages[0].rectTransform.localScale.y;

        fadeUI = GetComponent<FadeUI>();
        fadeUI.FadeOut(0);
    }

    public void Hit(bool headShot)
    {
        fadeUI.FadeIn(0);

        foreach (var image in hitMarkerImages)
        {
            image.color = headShot ? headHitColor : Color.white;
            image.rectTransform.localScale = new Vector3(image.rectTransform.localScale.x, headShot ? headHitWidth : originalWidth, image.rectTransform.localScale.z);
        }

        fadeUI.FadeOut(fadeOutTime, fadeOutDelay);
    }
}
