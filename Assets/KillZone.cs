using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillZone : MonoBehaviour
{
    const float DEATH_DAMAGE = 10000f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<Player>().TakeDamageAnonymous(DEATH_DAMAGE);
    }
}
