using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] float rotationAnglePerSecond = 10f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationAnglePerSecond * Time.deltaTime);
    }
}
