using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    [SerializeField] float speed = 100f;
    [SerializeField] float lifeTime = 1f;
    [SerializeField] float timeBeforeActivation = 0.1f;
    TrailRenderer trail;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        if (timeBeforeActivation > 0)
        {
            trail.enabled = false;
            this.Invoke(() => trail.enabled = true, timeBeforeActivation);
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += transform.forward * (speed * Time.deltaTime);
    }
}
