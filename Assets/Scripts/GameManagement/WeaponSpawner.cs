using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponSpawner : MonoBehaviour
{
    [SerializeField] private WeaponPickup weaponPrefab;
    [SerializeField] private float spawnTimer = 30f;

    public void SpawnWeaponOnStart()
    {
        SpawnWeapon();
    }

    private void SpawnWeapon()
    {
        if (Net.IsServer)
        {
            WeaponPickup weapon = Instantiate(weaponPrefab, transform.position, Quaternion.identity);
            weapon.GetComponent<NetworkObject>().Spawn();
            weapon.Spawned(this);
        }
    }

    IEnumerator SpawnWeaponTimer()
    {
        yield return new WaitForSeconds(spawnTimer);
        SpawnWeapon();
    }

    public void WeaponPickedUp()
    {
        StartCoroutine(SpawnWeaponTimer());
    }
}
