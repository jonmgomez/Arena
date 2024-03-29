using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDrawRayForward : MonoBehaviour
{
    [SerializeField] private Color color = Color.red;
    [SerializeField] private float length = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawRay(transform.position, transform.forward * length);
    }
}
