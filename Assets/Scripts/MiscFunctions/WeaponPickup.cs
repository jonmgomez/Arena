using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponPickup : NetworkBehaviour
{
    [SerializeField] int weaponId;

    WeaponSpawner originalSpawner;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            var player = other.transform.root.GetComponent<PlayerWeapon>();
            player.PickupWeapon(weaponId);
            PickedUpWeaponServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickedUpWeaponServerRpc()
    {
        GetComponent<NetworkObject>().Despawn(true);

        if (originalSpawner != null)
        {
            originalSpawner.WeaponPickedUp();
        }
        else
        {
            Logger.Default.LogError("Original spawner is null!");
        }
    }

    public void Spawned(WeaponSpawner weaponSpawner)
    {
        originalSpawner = weaponSpawner;
    }
}
