using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pistol : Weapon
{
    const float MAX_DISTANCE = 100f;

    [Header("Pistol")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] Transform muzzle;
    [SerializeField] LayerMask layerMask;
    [SerializeField] BulletTrail trail;
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] PlayerCamera playerCamera;

    [SerializeField] Bloom bloom;

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

    protected override void Start()
    {
        if (!IsOwner) return;
        base.Start();

        idlePosition = transform.localPosition;
        idleRotation = transform.localRotation;
        crosshair = FindObjectOfType<Crosshair>(true);
    }

    protected override void Update()
    {
        if (!IsOwner) return;
        base.Update();

        transform.localPosition = Vector3.Lerp(transform.localPosition, recoilTargetPosition, Time.deltaTime * 20f);
        recoilTargetPosition = Vector3.Lerp(recoilTargetPosition, idlePosition, Time.deltaTime * 10f);

        transform.localRotation = Quaternion.Lerp(transform.localRotation, recoilTargetRotation, Time.deltaTime * 20f);
        recoilTargetRotation = Quaternion.Lerp(recoilTargetRotation, idleRotation, Time.deltaTime * 10f);
    }

    protected override void OnFire()
    {
        Recoil();

        Vector3 direction = firePoint.forward;
        if (!IsAimedIn())
        {
            //crosshair.Bloom(bloomPerShotPercent);
            direction = bloom.AdjustForBloom(direction);

            bloom.AddBloom(GetBloom());
        }

        Vector3 hitPosition = firePoint.position + direction * MAX_DISTANCE;
        if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            hitPosition = hit.point;
            if (hit.transform.tag == "Player")
            {
                hit.transform.GetComponent<Player>().TakeDamage(GetDamage(), OwnerClientId);
            }
        }

        SpawnFiringEffects(hitPosition);
        SpawnFiringEffectsServerRpc(hitPosition);

        recoilTargetPosition = idlePosition - firePoint.forward * Random.Range(0.1f, 0.2f);
        recoilTargetRotation = idleRotation * Quaternion.Euler(Random.Range(-25f, -10f), 0, 0f);
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnFiringEffectsServerRpc(Vector3 hitPosition)
    {
        SpawnFiringEffectsClientRpc(hitPosition);
    }

    [ClientRpc]
    void SpawnFiringEffectsClientRpc(Vector3 hitPosition)
    {
        if (!IsOwner)
            SpawnFiringEffects(hitPosition);
    }

    void SpawnFiringEffects(Vector3 hitPosition)
    {
        var bullet = Instantiate(trail, muzzle.position, firePoint.rotation);
        bullet.SetEndPosition(hitPosition);
        var obj = Instantiate(muzzleFlash, muzzle.position, firePoint.rotation);
        obj.transform.parent = muzzle;
    }

    private void Recoil()
    {
        (float horizontal, float vertical) = GetRecoil();
        float x = Random.Range(-horizontal, horizontal);
        float y = vertical;
        playerCamera.Rotate(x, y);
    }
}
