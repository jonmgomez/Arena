using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillZone : MonoBehaviour
{
    const float DEATH_DAMAGE = 10000f;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            Player player = other.transform.root.GetComponent<Player>();

            if (player.IsOwner) // Clients calculate their own damage (avoids some interpolation-caused issues)
                player.TakeDamageAnonymous(DEATH_DAMAGE);
        }
    }
}
