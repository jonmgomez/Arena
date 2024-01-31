using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    [SerializeField] float speed = 100f;
    [SerializeField] float timeBeforeActivation = 0.1f;
    [SerializeField] Destroy destroyAfter;
    TrailRenderer trail;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        if (timeBeforeActivation > 0)
        {
            trail.enabled = false;
            this.Invoke(() => trail.enabled = true, timeBeforeActivation);
        }
    }

    void Update()
    {
        transform.position += transform.forward * (speed * Time.deltaTime);
    }

    public void SetEndPosition(Vector3 position)
    {
        transform.LookAt(position);
        float timeToMeet = Vector3.Distance(transform.position, position) / speed;
        destroyAfter.SetDestroyTimer(timeToMeet);
    }
}
