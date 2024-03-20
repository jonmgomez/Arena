using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Offsets the position of the object this script is attached to
/// </summary>
public class Offset : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    private Vector3 originalOffset;

    void Start()
    {
        originalOffset = transform.localPosition;
        transform.localPosition = originalOffset + offset;
    }

    // Only for editor, makes it easier to see/change the offset
    #if UNITY_EDITOR
    void Update()
    {
        transform.localPosition = originalOffset + offset;
    }
    #endif
}
