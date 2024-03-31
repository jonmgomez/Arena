using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Crosshair : MonoBehaviour
{
    private enum CrosshairState
    {
        Idle,
        Blooming,
        Bloomed,
        Recovering
    }

    private class CrosshairBar
    {
        public Transform mTransform;
        public Vector3 mIdlePosition;
        public Vector3 mBloomTargetPosition;

        public void MoveTowardsIdlePosition(float distance)
        {
            mTransform.localPosition = Vector3.MoveTowards(mTransform.localPosition, mIdlePosition, distance);
            mBloomTargetPosition = mTransform.localPosition;
        }

        public void MoveTowardsBloomPosition(float distance)
        {
            mTransform.localPosition = Vector3.MoveTowards(mTransform.localPosition, mBloomTargetPosition, distance);
        }

        public bool AtIdlePosition()
        {
            return Vector3.Distance(mTransform.localPosition, mIdlePosition) < 0.01f;
        }

        public bool AtBloomPosition()
        {
            return Vector3.Distance(mTransform.localPosition, mBloomTargetPosition) < 0.01f;
        }
    }

    [SerializeField] Transform[] initialCrosshairBars;
    [SerializeField] Transform crosshairCenter;
    [SerializeField] float bloomMaximum = 3.5f;
    float currentBloomPercentage = 0f;
    [SerializeField] bool bloomInstantly = false;
    [SerializeField] float bloomSpeed = 100f;
    [SerializeField] float recoverSpeed = 20f;
    [SerializeField] float bloomRecoveryDelaySeconds = 1f;

    CrosshairBar[] crosshairBars;
    [SerializeField] CrosshairState state = CrosshairState.Idle;

    void Start()
    {
        crosshairBars = new CrosshairBar[initialCrosshairBars.Length];
        for (int i = 0; i < crosshairBars.Length; i++)
        {
            crosshairBars[i] = new CrosshairBar{ mTransform = initialCrosshairBars[i] };
            crosshairBars[i].mIdlePosition = crosshairBars[i].mTransform.localPosition;
            crosshairBars[i].mBloomTargetPosition = crosshairBars[i].mTransform.localPosition;
        }
    }

    void Update()
    {
        if (state != CrosshairState.Idle && state != CrosshairState.Bloomed)
            UpdateCrosshairPositions();
    }

    void UpdateCrosshairPositions()
    {
        if (state == CrosshairState.Blooming)
        {
            float distance = bloomSpeed * Time.deltaTime;
            Array.ForEach(crosshairBars, (bar) => bar.MoveTowardsBloomPosition(distance));
            if (crosshairBars[0].AtBloomPosition())
            {
                FinishedBloom();
            }
        }
        else
        {
            float distance = recoverSpeed * Time.deltaTime;
            Array.ForEach(crosshairBars, (bar) => bar.MoveTowardsIdlePosition(distance));
            if (crosshairBars[0].AtIdlePosition())
            {
                state = CrosshairState.Idle;
                currentBloomPercentage = 0f;
            }

            float maxDistance = Vector3.Distance(crosshairBars[0].mIdlePosition, crosshairBars[0].mIdlePosition * bloomMaximum);
            float currentDistance = Vector3.Distance(crosshairBars[0].mIdlePosition, crosshairBars[0].mTransform.localPosition);
            currentBloomPercentage = currentDistance / maxDistance;
        }
    }

    void FinishedBloom()
    {
        state = CrosshairState.Bloomed;
        StartCoroutine(RecoverFromBloom());
    }

    IEnumerator RecoverFromBloom()
    {
        yield return new WaitForSeconds(bloomRecoveryDelaySeconds);
        state = CrosshairState.Recovering;
    }

    public void Bloom(float bloomPercentage)
    {
        if (bloomPercentage < 0f || bloomPercentage > 1f)
            throw new ArgumentException("Bloom percentage must be between 0 and 1", "bloomPercentage");

        StopCoroutine(RecoverFromBloom());

        currentBloomPercentage = Mathf.Clamp(currentBloomPercentage + bloomPercentage, 0f, 1f);
        float bloom = (bloomMaximum - 1f)  * currentBloomPercentage + 1f;
        if (bloomInstantly)
        {
            Array.ForEach(crosshairBars, (bar) => {
                bar.mBloomTargetPosition = bar.mIdlePosition * bloom;
                bar.mTransform.localPosition = bar.mBloomTargetPosition;
            });
            FinishedBloom();
            return;
        }
        else
        {
            state = CrosshairState.Blooming;
            Array.ForEach(crosshairBars, (bar) => {
                bar.mBloomTargetPosition = bar.mIdlePosition * bloom;
            });
        }
    }

    public void SetBloomRecoveryDelay(float delay)
    {
        bloomRecoveryDelaySeconds = delay;
    }

    public void SetVisible(bool visible)
    {
        foreach (CrosshairBar crosshairBar in crosshairBars)
        {
            crosshairBar.mTransform.gameObject.SetActive(visible);
        }
        crosshairCenter.gameObject.SetActive(visible);
    }
}
