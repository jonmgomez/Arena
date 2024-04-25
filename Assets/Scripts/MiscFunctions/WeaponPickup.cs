using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponPickup : NetworkBehaviour
{
    [SerializeField] private int weaponId;

    private readonly List<PlayerWeaponPickupController> collidingPlayers = new();
    private WeaponSpawner originalSpawner;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            var player = other.transform.root.GetComponent<PlayerWeaponPickupController>();
            player.PickupInRange(this);
            collidingPlayers.Add(player);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            var player = other.transform.root.GetComponent<PlayerWeaponPickupController>();
            player.PickupOutOfRange(this);
            collidingPlayers.Remove(player);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var player in collidingPlayers)
        {
            player.PickupOutOfRange(this);
        }
    }

    public void PickupWeapon()
    {
        PickedUpWeaponServerRpc();
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

    public int GetWeaponId() => weaponId;
}
