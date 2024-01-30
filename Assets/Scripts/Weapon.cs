using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Profiling;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    const int LEFT_MOUSE_BUTTON = 0;
    const int RIGHT_MOUSE_BUTTON = 1;

    PlayerCamera playerCamera = null;

    [Header("General")]
    [SerializeField] bool canADS = false;
    bool aimedIn = false;
    [SerializeField] float fireRate = 0.1f;
    bool canFire = true;
    [SerializeField] bool isAutomatic = false;

    [SerializeField] float bloomPerShotPercent = 0.1f;
    [SerializeField] float recoilVerticalAmount = 0.1f;
    [SerializeField] float recoilRecoveryRate = 0.1f;
    [SerializeField] float recoilHorizontalAmount = 0.1f;

    [Header("Spawn Points")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] Transform muzzle;

    [Header("Spawn Prefabs")]
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] GameObject muzzleFlashPrefab;
    Bloom bloom;

    Crosshair crosshair;

    protected abstract void OnFire();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
        }
    }

    protected virtual void Start()
    {
        if (!IsOwner) return;

        playerCamera = transform.root.GetComponentInChildren<PlayerCamera>();
        crosshair = FindObjectOfType<Crosshair>(true);
        bloom = GetComponent<Bloom>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (!IsOwner) return;

        CheckAim();

        if (CheckFire())
        {
            if (fireRate > 0)
                StartCoroutine(FireRateCooldown());

            Fire();

            CalculateRecoil();
            CalculateBloom();
        }
    }

    private void CheckAim()
    {
        if (!canADS) return;

        if (Input.GetMouseButtonDown(RIGHT_MOUSE_BUTTON))
        {
            aimedIn = true;
        }
        else if (Input.GetMouseButtonUp(RIGHT_MOUSE_BUTTON))
        {
            aimedIn = false;
        }
    }

    private bool CheckFire()
    {
        if (!canFire) return false;

        if (isAutomatic && Input.GetMouseButton(LEFT_MOUSE_BUTTON))
        {
            return true;
        }
        else if (!isAutomatic && Input.GetMouseButtonDown(LEFT_MOUSE_BUTTON))
        {
            return true;
        }

        return false;
    }

    private void Fire()
    {
        Vector3 direction = firePoint.forward;
        if (!IsAimedIn())
        {
            //crosshair.Bloom(bloomPerShotPercent);
            direction = bloom.AdjustForBloom(direction);

            bloom.AddBloom(GetBloom());
        }

        SpawnProjectileNetworked(firePoint.position, direction, OwnerClientId);

        OnFire();
    }

    private void CalculateRecoil()
    {
        float x = Random.Range(-recoilHorizontalAmount, recoilHorizontalAmount);
        float y = recoilVerticalAmount;
        playerCamera.Rotate(x, y);
    }

    private void CalculateBloom()
    {
        crosshair.Bloom(bloomPerShotPercent);
    }

    public bool IsAimedIn() => aimedIn;
    public float GetBloom() => bloomPerShotPercent;

    IEnumerator FireRateCooldown()
    {
        canFire = false;
        yield return new WaitForSeconds(fireRate);
        canFire = true;
    }

        #region "Projectile Spawning"
    [ServerRpc(RequireOwnership = false)]
    void SpawnProjectileServerRpc(Vector3 spawn, Vector3 direction, ulong firedFromClientId)
    {
        SpawnProjectileClientRpc(spawn, direction, firedFromClientId);
    }

    [ClientRpc]
    void SpawnProjectileClientRpc(Vector3 spawn, Vector3 direction, ulong firedFromClientId)
    {
        if (!IsOwner)
        {
            SpawnProjectile(spawn, direction, firedFromClientId);
        }
    }

    private void SpawnProjectileNetworked(Vector3 spawn, Vector3 direction, ulong firedFromClientId)
    {
        SpawnProjectile(spawn, direction, firedFromClientId);
        SpawnProjectileServerRpc(spawn, direction, firedFromClientId);
    }

    private void SpawnProjectile(Vector3 spawn, Vector3 direction, ulong firedFromClientId)
    {
        var bullet = Instantiate(projectilePrefab, spawn, Quaternion.LookRotation(direction));
        bullet.SetFiredFromClient(IsServer, IsHost, firedFromClientId);
        SpawnFiringEffects();
    }
    #endregion

    private void SpawnFiringEffects()
    {
        // Muzzle flash
        var obj = Instantiate(muzzleFlashPrefab, muzzle.position, firePoint.rotation);
        obj.transform.parent = muzzle;
    }
}
