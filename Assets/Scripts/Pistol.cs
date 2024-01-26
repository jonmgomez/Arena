using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pistol : NetworkBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] Transform muzzle;
    [SerializeField] LayerMask layerMask;
    [SerializeField] GameObject trail;
    [SerializeField] GameObject muzzleFlash;

    [SerializeField] float damage = 10f;
    [SerializeField] float fireRate = 0.1f;
    [SerializeField] float bloomPerShotPercent = 0.1f;

    Vector3 idlePosition;
    Vector3 recoilTargetPosition;
    Quaternion idleRotation;
    Quaternion recoilTargetRotation;

    Crosshair crosshair;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
        }
    }

    void Start()
    {
        idlePosition = transform.localPosition;
        idleRotation = transform.localRotation;
        crosshair = FindObjectOfType<Crosshair>(true);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            crosshair.Bloom(bloomPerShotPercent);
            if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                hit.transform.GetComponent<Player>().TakeDamage(damage, OwnerClientId);
            }

            SpawnFiringEffects();
            SpawnFiringEffectsServerRpc();

            recoilTargetPosition = idlePosition - firePoint.forward * Random.Range(0.1f, 0.2f);
            recoilTargetRotation = idleRotation * Quaternion.Euler(Random.Range(-25f, -10f), 0, 0f);
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, recoilTargetPosition, Time.deltaTime * 20f);
        recoilTargetPosition = Vector3.Lerp(recoilTargetPosition, idlePosition, Time.deltaTime * 10f);

        transform.localRotation = Quaternion.Lerp(transform.localRotation, recoilTargetRotation, Time.deltaTime * 20f);
        recoilTargetRotation = Quaternion.Lerp(recoilTargetRotation, idleRotation, Time.deltaTime * 10f);
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnFiringEffectsServerRpc()
    {
        SpawnFiringEffectsClientRpc();
    }

    [ClientRpc]
    void SpawnFiringEffectsClientRpc()
    {
        if (!IsOwner)
            SpawnFiringEffects();
    }

    void SpawnFiringEffects()
    {
        Instantiate(trail, muzzle.position, firePoint.rotation);
        var obj = Instantiate(muzzleFlash, muzzle.position, firePoint.rotation);
        obj.transform.parent = muzzle;
    }
}
