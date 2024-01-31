using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    [SerializeField] float lifeTime = 1f;
    Coroutine destroyCoroutine;

    void Start()
    {
        // SetDestroyTimer may be called after Instantiate and before start!
        destroyCoroutine ??= StartCoroutine(DestroyAfter(lifeTime));
    }

    IEnumerator DestroyAfter(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }

    public void SetDestroyTimer(float time)
    {
        lifeTime = time;

        if (destroyCoroutine != null)
            StopCoroutine(destroyCoroutine);

        destroyCoroutine = StartCoroutine(DestroyAfter(lifeTime));
    }
}
