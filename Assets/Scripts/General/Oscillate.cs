using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oscillate : MonoBehaviour
{
    [SerializeField] float speed = 1f;
    [SerializeField] float amplitude = 1f;
    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float verticalMovement = Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = startPos + new Vector3(0, verticalMovement, 0);
    }
}
