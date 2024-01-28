using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    [SerializeField] float lifeTime = 1f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
