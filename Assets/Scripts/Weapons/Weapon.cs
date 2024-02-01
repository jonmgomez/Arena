using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Profiling;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    const int LEFT_MOUSE_BUTTON = 0;

    WeaponState currentState;

    [Header("General")]
    public bool CanADS = false;
    public float FireRate = 0.1f;
    public bool IsAutomatic = false;
    [NonSerialized] public bool AimedIn = false;
    [NonSerialized] public bool AttemptingFire = false;

    public float ReloadTime = 1f;
    public float AutoReloadDelay = 0.5f;
    public int   Ammo       = 30;
    [NonSerialized] public int MaxAmmo = 30;

    [SerializeField] int projectilesPerShot = 1;

    public float BloomPerShotPercent    = 0.1f;
    public float RecoilVerticalAmount   = 0.1f;
    public float RecoilRecoveryRate     = 0.1f;
    public float RecoilHorizontalAmount = 0.1f;

    [Header("Spawn Points")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] Transform muzzle;

    [Header("Spawn Prefabs")]
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] GameObject muzzleFlashPrefab;

    [NonSerialized] public PlayerCamera PlayerCamera;
    [NonSerialized] public Crosshair Crosshair;
    [NonSerialized] public Bloom Bloom;

    protected abstract void OnFire();

    public void SetState(WeaponState state)
    {
        currentState.OnStateExit();
        currentState = state;
        currentState.OnStateEnter();
    }

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

        MaxAmmo = Ammo;
        PlayerCamera = transform.root.GetComponentInChildren<PlayerCamera>();
        Crosshair = FindObjectOfType<Crosshair>(true);
        Bloom = GetComponent<Bloom>();

        currentState = new WeaponReadyState(this);
        currentState.OnStateEnter();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (!IsOwner) return;

        AttemptingFire = IsAutomatic && Input.GetMouseButton(LEFT_MOUSE_BUTTON) ||
                         !IsAutomatic && Input.GetMouseButtonDown(LEFT_MOUSE_BUTTON);

        //Debug.Log(currentState);
        currentState.Update();
    }

    public void Fire()
    {
        for (int i = 0; i < projectilesPerShot; i++)
        {
            Vector3 direction = firePoint.forward;
            if (!AimedIn)
            {
                direction = Bloom.AdjustForBloom(direction);
            }

            SpawnProjectileNetworked(firePoint.position, direction, OwnerClientId);
        }

        OnFire();
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
