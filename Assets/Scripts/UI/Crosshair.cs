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
    }

    [SerializeField] private Transform[] initialCrosshairBars;
    [SerializeField] private Transform crosshairCenter;

    [SerializeField] private float spreadScaler = 1f;
    [SerializeField] private float currentSpread = 0f;

    private CrosshairBar[] crosshairBars;

    void Start()
    {
        crosshairBars = new CrosshairBar[initialCrosshairBars.Length];
        for (int i = 0; i < crosshairBars.Length; i++)
        {
            crosshairBars[i] = new CrosshairBar{ Transform = initialCrosshairBars[i] };
            crosshairBars[i].IdlePosition = crosshairBars[i].Transform.localPosition;
        }
    }

    public void SetSpread(float spreadAngle)
    {
        currentSpread = spreadAngle * spreadScaler;
        foreach (CrosshairBar crosshairBar in crosshairBars)
        {
            crosshairBar.Transform.localPosition = crosshairBar.IdlePosition + crosshairBar.IdlePosition.normalized * currentSpread;
        }
    }

    public void SetVisible(bool visible)
    {
        foreach (CrosshairBar crosshairBar in crosshairBars)
        {
            crosshairBar.Transform.gameObject.SetActive(visible);
        }
        crosshairCenter.gameObject.SetActive(visible);
    }
}
