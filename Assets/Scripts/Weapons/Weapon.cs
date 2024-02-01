using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Profiling;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    const int LEFT_MOUSE_BUTTON = 0;
    const int RIGHT_MOUSE_BUTTON = 1;

    [Header("General")]
    [SerializeField] bool canADS  = false;
                     bool aimedIn = false;

    [SerializeField] float fireRate = 0.1f;
                     bool canFire   = true;

    [SerializeField] bool isAutomatic = false;

    [SerializeField] float reloadTime = 1f;
    [SerializeField] int ammo         = 30;
                     int maxAmmo      = 30;
                     bool reloading   = false;

    [SerializeField] int projectilesPerShot = 1;

    [SerializeField] float bloomPerShotPercent    = 0.1f;
    [SerializeField] float recoilVerticalAmount   = 0.1f;
    [SerializeField] float recoilRecoveryRate     = 0.1f;
    [SerializeField] float recoilHorizontalAmount = 0.1f;

    [Header("Spawn Points")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] Transform muzzle;

    [Header("Spawn Prefabs")]
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] GameObject muzzleFlashPrefab;

    PlayerCamera playerCamera;
    Crosshair crosshair;
    Bloom bloom;

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

        maxAmmo = ammo;
        playerCamera = transform.root.GetComponentInChildren<PlayerCamera>();
        crosshair = FindObjectOfType<Crosshair>(true);
        bloom = GetComponent<Bloom>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (!IsOwner) return;

        CheckReload();
        if (reloading) return;

        CheckAim();

        if (CheckIsFiring())
        {
            if (fireRate > 0)
                StartCoroutine(FireRateCooldown());

            Fire();

            if (!IsAimedIn())
            {
                CalculateRecoil();
                CalculateBloom();
            }
        }
    }

    private void CheckReload()
    {
        if (reloading) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            reloading = true;
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        yield return new WaitForSeconds(1f);
        reloading = false;
        ammo = maxAmmo;
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

    private bool CheckIsFiring()
    {
        if (!canFire) return false;
        if (ammo <= 0) return false;

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
        ammo--;
        for (int i = 0; i < projectilesPerShot; i++)
        {
            Vector3 direction = firePoint.forward;
            if (!IsAimedIn())
            {
                direction = bloom.AdjustForBloom(direction);
            }

            SpawnProjectileNetworked(firePoint.position, direction, OwnerClientId);
        }

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
        bloom.AddBloom(bloomPerShotPercent);
    }

    public bool IsAimedIn() => aimedIn;

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
        bullet.SetSpawnDetails(spawn, muzzle.position);
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
