using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pistol : NetworkBehaviour
{
    const int LEFT_MOUSE_BUTTON = 0;
    const int RIGHT_MOUSE_BUTTON = 1;
    const float MAX_DISTANCE = 100f;

    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] Transform muzzle;
    [SerializeField] LayerMask layerMask;
    [SerializeField] BulletTrail trail;
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] PlayerCamera playerCamera;

    [SerializeField] float damage = 10f;
    [SerializeField] float fireRate = 0.1f;
    [SerializeField] float bloomPerShotPercent = 0.1f;

    [SerializeField] float recoilVerticalAmount = 0.1f;
    [SerializeField] float recoilRecoveryRate = 0.1f;
    [SerializeField] float recoilHorizontalAmount = 0.1f;

    [SerializeField] Bloom bloom;
    bool aimedIn = false;

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
        if (Input.GetMouseButtonDown(RIGHT_MOUSE_BUTTON))
        {
            aimedIn = true;
        }
        else if (Input.GetMouseButtonUp(RIGHT_MOUSE_BUTTON))
        {
            aimedIn = false;
        }

        if (Input.GetMouseButtonDown(LEFT_MOUSE_BUTTON))
        {
            Recoil();

            Vector3 direction = firePoint.forward;
            if (!aimedIn)
            {
                //crosshair.Bloom(bloomPerShotPercent);
                direction = bloom.AdjustForBloom(direction);

                bloom.AddBloom(bloomPerShotPercent);
            }

            Vector3 hitPosition = firePoint.position + direction * MAX_DISTANCE;
            if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                hitPosition = hit.point;
                if (hit.transform.tag == "Player")
                {
                   hit.transform.GetComponent<Player>().TakeDamage(damage, OwnerClientId);
                }
            }

            SpawnFiringEffects(hitPosition);
            SpawnFiringEffectsServerRpc(hitPosition);

            recoilTargetPosition = idlePosition - firePoint.forward * Random.Range(0.1f, 0.2f);
            recoilTargetRotation = idleRotation * Quaternion.Euler(Random.Range(-25f, -10f), 0, 0f);
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, recoilTargetPosition, Time.deltaTime * 20f);
        recoilTargetPosition = Vector3.Lerp(recoilTargetPosition, idlePosition, Time.deltaTime * 10f);

        transform.localRotation = Quaternion.Lerp(transform.localRotation, recoilTargetRotation, Time.deltaTime * 20f);
        recoilTargetRotation = Quaternion.Lerp(recoilTargetRotation, idleRotation, Time.deltaTime * 10f);
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
        float x = Random.Range(-recoilHorizontalAmount, recoilHorizontalAmount);
        float y = recoilVerticalAmount;
        playerCamera.Rotate(x, y);
    }
}
