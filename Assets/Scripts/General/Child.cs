using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Child : MonoBehaviour
{
    [SerializeField] private Transform simulatedParent;
    [SerializeField] private bool followPosition = true;
    [SerializeField] private bool followRotation = true;

    private Vector3 originalLocalPosition;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
    }

    void Update()
    {
        if (simulatedParent != null)
        {
            if (followPosition)
                transform.position = simulatedParent.position;

            if (followRotation)
                transform.rotation = simulatedParent.rotation;
        }
    }
}
