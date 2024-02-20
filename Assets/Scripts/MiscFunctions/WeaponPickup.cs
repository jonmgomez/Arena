using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponPickup : NetworkBehaviour
{
    [SerializeField] int weaponId;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerWeapon>();
            player.PickupWeapon(weaponId);
            PickedUpWeaponServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickedUpWeaponServerRpc()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }
}
