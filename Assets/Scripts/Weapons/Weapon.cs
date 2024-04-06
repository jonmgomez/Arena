using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    const int LEFT_MOUSE_BUTTON = 0;

    [Header("General")]
    [Tooltip("The name of the weapon.")]
    public string Name = "Weapon";
    [Tooltip("If true this weapon is able to aim down sights.")]
    public bool CanADS = false;
    [Tooltip("The position offset when aiming down sights.")]
    [SerializeField] private Vector3 adsPositionOffset;
    [Tooltip("Delay in seconds before the weapon can fire again.")]
    public float FireRate = 0.1f;
    [Tooltip("If true, the weapon will fire automatically when the fire button is held down.")]
    public bool IsAutomatic = false;
    [Tooltip("The number of projectiles to spawn per shot.")]
    [SerializeField] private int projectilesPerShot = 1;
    [Tooltip("The maximum angle spread of the weapon when stationary and hip firing.")]
    [SerializeField] private float stationaryHipFireSpreadAngle = 2f;
    [Tooltip("The maximum angle spread of the weapon when moving at max velocity and hip firing.")]
    [SerializeField] private float fullMovementHipFireSpreadAngle = 3f;


    [Header("Reload")]
    [Tooltip("The current ammo count / starting ammo count of the weapon.")]
    [SerializeField] private int ammo = 0;
                     public int Ammo { get => ammo; set { ammo = value; OnAmmoChanged?.Invoke(ammo); } }
    [Tooltip("The time it takes to reload the weapon. This replenishes the ammo count at this time regardless of the animation.")]
    public float ReloadTime = 1f;
    [Tooltip("The time it takes to reload when the weapon is empty. This replenishes the ammo count at this time regardless of the animation.")]
    public float EmptyReloadTime = 1f;
    [Tooltip("The delay before the weapon will automatically reload after firing the last bullet and attempting to fire again.")]
    public float AutoReloadDelay = 0.5f;
    [Tooltip("If true, the weapon will reload in separate bullets instead of all at once. " +
             "Uses the ReloadStart, One, and End animations instead of Reload and ReloadEmpty")]
    public bool ReloadSingles = false;
    [Tooltip("The amount of ammo to replenish per reload when ReloadSingles is true.")]
    public int ReloadSinglesAmount = 1;

    [Header("Animation")]
    public Animator Animator;
    public GameObject ThirdPersonWeapon;
    public Animator ThirdPersonWeaponAnimator;

    [Header("Spawn Points")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform thirdPersonFirePoint;
    [SerializeField] private Transform thirdPersonMuzzle;

    [Header("Spawn Prefabs")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;



    private WeaponState[] states = new WeaponState[Enum.GetNames(typeof(WeaponState.State)).Length];
    private WeaponState currentState;
    private WeaponRecoil recoilController;
    private Renderer[] renderers = new Renderer[0];
    private Renderer[] thirdPersonModelRenderers = new Renderer[0];

    public event Action<int> OnAmmoChanged;

    [NonSerialized] public Player Player;
    [NonSerialized] public PlayerWeaponAnimator WeaponAnimator;
    [NonSerialized] public PlayerCamera PlayerCamera;
    [NonSerialized] public Crosshair Crosshair;
    [NonSerialized] public AimDownSightsViewer ADSViewer;

    [NonSerialized] public int MaxAmmo = 30;
    [NonSerialized] public bool AimedIn = false;
    [NonSerialized] public bool AttemptingFire = false;

    private float hipFireSpreadAngle;

    protected abstract void OnFire();

    public void SetState(WeaponState.State stateEnum)
    {
        WeaponState state = states[(int) stateEnum];
        WeaponState.State previousState = currentState.GetStateType();
        currentState.OnStateExit();
        currentState = state;
        currentState.OnStateEnter(previousState);
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
            new WeaponReadyingState(this),
            new WeaponEmptyState(this),
            new WeaponRecoveringState(this),
            new WeaponReloadingState(this),
            new WeaponReadyState(this),
            new WeaponDisabledState(this)
        };
        currentState = states[(int) WeaponState.State.Readying];
        Debug.Assert(states.Length == Enum.GetNames(typeof(WeaponState.State)).Length, "Weapon states are not equal to the number of states in the WeaponState enum.");

        Player = transform.root.GetComponent<Player>();
        WeaponAnimator = transform.root.GetComponentInChildren<PlayerWeaponAnimator>();
        PlayerCamera = transform.root.GetComponentInChildren<PlayerCamera>();
        Crosshair = FindObjectOfType<Crosshair>(true);
        ADSViewer = transform.root.GetComponentInChildren<AimDownSightsViewer>();

        recoilController = GetComponent<WeaponRecoil>();
        renderers = GetComponentsInChildren<Renderer>();

        if (ThirdPersonWeapon != null)
        {
            thirdPersonModelRenderers = ThirdPersonWeapon.GetComponentsInChildren<Renderer>();
        }
    }

    protected virtual void Start()
    {
        if (!IsOwner) return;

        hipFireSpreadAngle = stationaryHipFireSpreadAngle;
        currentState.OnStateEnter(WeaponState.State.Disabled);
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
                direction = Quaternion.Euler(UnityEngine.Random.Range(-hipFireSpreadAngle, hipFireSpreadAngle),
                                             UnityEngine.Random.Range(-hipFireSpreadAngle, hipFireSpreadAngle),
                                             UnityEngine.Random.Range(-hipFireSpreadAngle, hipFireSpreadAngle)) * direction;
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
            SpawnFiringEffects(thirdPersonMuzzle, thirdPersonFirePoint);
        }
    }

    private void SpawnProjectileNetworked(Vector3 spawn, Vector3 direction, ulong firedFromClientId)
    {
        // On the local client the prefab instantiate on the first person model.
        // On other clients the prefab instantiates on the third person model.
        SpawnProjectile(firePoint.position, direction, firedFromClientId);
        SpawnFiringEffects(muzzle, firePoint);

        SpawnProjectileServerRpc(thirdPersonFirePoint.position, direction, firedFromClientId);
    }

    private void SpawnProjectile(Vector3 spawn, Vector3 direction, ulong firedFromClientId)
    {
        var bullet = Instantiate(projectilePrefab, spawn, Quaternion.LookRotation(direction));
        bullet.SetFiredFromClient(IsServer, IsHost, firedFromClientId);
        bullet.SetSpawnDetails(spawn, muzzle.position);
    }
    #endregion

    private void SpawnFiringEffects(Transform instantiateAt, Transform rotationAt)
    {
        if (instantiateAt == null || rotationAt == null)
            return;

        // Muzzle flash
        var obj = Instantiate(muzzleFlashPrefab, instantiateAt.position, rotationAt.rotation);
        obj.transform.parent = instantiateAt;
    }

    public void SetEnabled(bool enabled)
    {
        if (IsOwner)
        {
            bool wasEnabled = !enabled && currentState.GetStateType() != WeaponState.State.Disabled;
            if (wasEnabled)
            {
                if (AimedIn)
                {
                    AimedIn = false;
                    ADSViewer.RestorePositions(this);
                }
            }

            if (enabled)
                SetState(DetermineState());
            else
                SetState(WeaponState.State.Disabled);
        }

        if (Player.ShowFirstPersonMesh())
            Array.ForEach(renderers, r => r.enabled = enabled);
        else
            Array.ForEach(thirdPersonModelRenderers, r => r.enabled = enabled);
    }

    public void Reset()
    {
        Ammo = MaxAmmo;
    }

    public void CalculateMovementHipFireSpread(float percentageOfMaxSpeed)
    {
        hipFireSpreadAngle = Mathf.Lerp(stationaryHipFireSpreadAngle, fullMovementHipFireSpreadAngle, percentageOfMaxSpeed);
        Crosshair.SetSpread(hipFireSpreadAngle);
    }

    public float GetFireRate() => FireRate;
    public Vector3 GetAimPositionOffset() => adsPositionOffset;
}
