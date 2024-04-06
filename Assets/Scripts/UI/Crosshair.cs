using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Crosshair : MonoBehaviour
{
    private class CrosshairBar
    {
        public Transform Transform;
        public Vector3 IdlePosition;
        public Vector3 TargetPosition;
    }

    [SerializeField] private Transform[] initialCrosshairBars;
    [SerializeField] private Transform crosshairCenter;

    [SerializeField] private float visibilityFadeTime = 0.25f;

    [SerializeField] private float spreadScaler = 1f;
    [SerializeField] private float currentSpread = 0f;
    [SerializeField] private float spreadSmoothTime = 0.5f;

    private FadeUI fade;
    private CrosshairBar[] crosshairBars;
    private float spreadAnimationVelocity;

    void Start()
    {
        fade = GetComponent<FadeUI>();
        fade.FadeIn(0f);

        crosshairBars = new CrosshairBar[initialCrosshairBars.Length];
        for (int i = 0; i < crosshairBars.Length; i++)
        {
            crosshairBars[i] = new CrosshairBar{ Transform = initialCrosshairBars[i] };
            crosshairBars[i].IdlePosition = crosshairBars[i].Transform.localPosition;
            Debug.Log(crosshairBars[i].IdlePosition);
        }
    }

    void Update()
    {
        foreach (CrosshairBar crosshairBar in crosshairBars)
        {
            crosshairBar.Transform.localPosition = Vector3.MoveTowards(crosshairBar.Transform.localPosition, crosshairBar.TargetPosition, spreadAnimationVelocity * Time.deltaTime);
        }
    }

    public void SetSpread(float spreadAngle)
    {
        if (spreadAngle * spreadScaler == currentSpread) return;

        currentSpread = spreadAngle * spreadScaler;
        foreach (CrosshairBar crosshairBar in crosshairBars)
        {
            crosshairBar.TargetPosition = crosshairBar.IdlePosition + crosshairBar.IdlePosition.normalized * currentSpread;
        }
        spreadAnimationVelocity = Vector3.Distance(crosshairBars[0].Transform.localPosition, crosshairBars[0].TargetPosition) / spreadSmoothTime;
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            fade.FadeIn(visibilityFadeTime);
        }
        else
        {
            fade.FadeOut(visibilityFadeTime);
        }
    }
}
