using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Profiling;
using UnityEditor.Animations;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    const int LEFT_MOUSE_BUTTON = 0;

    public event Action<int> OnAmmoChanged;

    WeaponState[] states = new WeaponState[Enum.GetNames(typeof(WeaponState.State)).Length];
    WeaponState currentState;
    WeaponRecoil recoilController;
    MeshRenderer[] renderers;

    [Header("General")]
    public string Name = "Weapon";
    public bool CanADS = false;
    public float FireRate = 0.1f;
    public bool IsAutomatic = false;
    [NonSerialized] public bool AimedIn = false;
    [NonSerialized] public bool AttemptingFire = false;

    public float ReloadTime = 1f;
    public float AutoReloadDelay = 0.5f;
    [SerializeField] private int ammo = 0;
                     public int Ammo { get => ammo; set { ammo = value; OnAmmoChanged?.Invoke(ammo); } }
    [NonSerialized] public int MaxAmmo = 30;

    [SerializeField] int projectilesPerShot = 1;

    public float BloomPerShotPercent    = 0.1f;

    public Animator Animator;

    [Header("Spawn Points")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] Transform muzzle;

    [Header("Spawn Prefabs")]
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] GameObject muzzleFlashPrefab;

    [NonSerialized] public PlayerWeaponAnimator WeaponAnimator;
    [NonSerialized] public PlayerCamera PlayerCamera;
    [NonSerialized] public Crosshair Crosshair;
    [NonSerialized] public Bloom Bloom;

    protected abstract void OnFire();

    public void SetState(WeaponState.State stateEnum)
    {
        WeaponState state = states[(int) stateEnum];
        currentState.OnStateExit();
        currentState = state;
        currentState.OnStateEnter();
    }

    public WeaponState.State DetermineState()
    {
        for (int i = 0; i < states.Length; i++)
        {
            // States are first checked in order of priority
            // ex. if out of ammo, we should enter the empty state first before the recovering state
            if (states[i].ShouldEnter())
            {
                return (WeaponState.State) i;
            }
        }
        return WeaponState.State.Ready;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
        }
    }

    protected virtual void Awake()
    {
        MaxAmmo = Ammo;

        states = new WeaponState[]
        {
            new WeaponEmptyState(this),
            new WeaponRecoveringState(this),
            new WeaponReloadingState(this),
            new WeaponReadyState(this),
            new WeaponDisabledState(this)
        };
        currentState = states[(int) WeaponState.State.Ready];
        Debug.Assert(states.Length == Enum.GetNames(typeof(WeaponState.State)).Length, "Weapon states are not equal to the number of states in the WeaponState enum.");

        WeaponAnimator = transform.root.GetComponentInChildren<PlayerWeaponAnimator>();
        PlayerCamera = transform.root.GetComponentInChildren<PlayerCamera>();
        Crosshair = FindObjectOfType<Crosshair>(true);
        Bloom = GetComponent<Bloom>();

        recoilController = GetComponent<WeaponRecoil>();
        renderers = GetComponentsInChildren<MeshRenderer>();
    }

    protected virtual void Start()
    {
        if (!IsOwner) return;

        currentState.OnStateEnter();
    }

    public virtual void WeaponUpdate()
    {
        if (!IsOwner) return;

        AttemptingFire = IsAutomatic && Input.GetMouseButton(LEFT_MOUSE_BUTTON) ||
                         !IsAutomatic && Input.GetMouseButtonDown(LEFT_MOUSE_BUTTON);

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

        if (recoilController != null)
            recoilController.CalculateRecoil();

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

    public void SetEnabled(bool enabled)
    {
        if (enabled)
            SetState(DetermineState());
        else
            SetState(WeaponState.State.Disabled);

        Array.ForEach(renderers, r => r.enabled = enabled);
    }

    public float GetFireRate() => FireRate;
}
