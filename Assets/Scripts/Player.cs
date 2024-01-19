using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] NetworkVariable<float> health = new(100f);

    void Update()
    {
        if (!IsOwner)
            return;
    }

    public void TakeDamage(float damage)
    {
        TakeDamageServerRpc(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    void TakeDamageServerRpc(float damage)
    {
        health.Value -= damage;
        if (health.Value <= 0f)
        {
            Debug.Log("Player died");
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
