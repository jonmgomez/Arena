using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bloom : MonoBehaviour
{
    [SerializeField] float maxBloomAngleDegrees = 10f;
    [SerializeField] float recoveryDelaySeconds = 0.1f;
    [SerializeField] float recoveryPercentPerSecond = 0.5f;
    [SerializeField] float currentBloom = 0f;

    Crosshair crosshair;
    Coroutine bloomRecoveryCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        crosshair = FindObjectOfType<Crosshair>(true);
        crosshair.SetBloomRecoveryDelay(recoveryDelaySeconds);
    }

    IEnumerator StartBloomRecovery()
    {
        yield return new WaitForSeconds(recoveryDelaySeconds);
        while (currentBloom > 0)
        {
            currentBloom -= recoveryPercentPerSecond * maxBloomAngleDegrees * Time.deltaTime;
            yield return null;
        }
        currentBloom = 0f;
    }

    public void AddBloom(float percentage)
    {
        percentage = Mathf.Clamp01(percentage);

        crosshair.Bloom(percentage);
        currentBloom += percentage * maxBloomAngleDegrees;
        currentBloom = Mathf.Clamp(currentBloom, 0f, maxBloomAngleDegrees);

        if (currentBloom > 0)
        {
            bloomRecoveryCoroutine = this.RestartCoroutine(StartBloomRecovery(), bloomRecoveryCoroutine);
        }
    }

    public Vector3 AdjustForBloom(Vector3 direction)
    {
        float randX = Random.Range(-currentBloom, currentBloom);
        float randY = Random.Range(-currentBloom, currentBloom);
        float randZ = Random.Range(-currentBloom, currentBloom);

        return Quaternion.AngleAxis(randY, Vector3.up) *
               Quaternion.AngleAxis(randX, Vector3.right) *
               Quaternion.AngleAxis(randZ, Vector3.forward) *
               direction;
    }
}
