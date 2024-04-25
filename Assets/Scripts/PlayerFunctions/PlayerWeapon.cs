using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    private Player player;
    [SerializeField] private Weapon activeWeapon;
    [SerializeField] private Weapon mainWeapon;
    [SerializeField] private Weapon sideWeapon;
    [SerializeField] private Weapon[] allWeapons;

    [SerializeField] private float maxSpeedForMaxSpread = 3f;

    private Weapon[] startingWeapons;
    private bool scriptEnabled = true;

    public event System.Action<Weapon> OnActiveWeaponChanged;

    private Crosshair crosshair;

    void Start()
    {
        startingWeapons = new Weapon[] { mainWeapon, sideWeapon };
        Debug.Assert(activeWeapon == mainWeapon || activeWeapon == sideWeapon, "Active weapon must be either the main or side weapon!");

        // Disable all weapons except the active weapon
        // Includes all non-owner clients
        foreach (var weapon in allWeapons)
        {
            if (weapon != activeWeapon)
                weapon.SetEnabled(false);
            else
                weapon.SetEnabled(true);

            weapon.OnAmmoChanged += (ammo) =>
            {
                if (weapon == activeWeapon)
                    player.HUD.UpdateCurrentAmmo(ammo);
            };
        }

        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        player = GetComponent<Player>();
        player.HUD.UpdateWeapon(activeWeapon.Name, activeWeapon.Ammo, activeWeapon.MaxAmmo);

        crosshair = player.GetPlayerHUD().GetCrosshair();
        crosshair.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!IsOwner) return;

        if (!scriptEnabled) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetActiveWeapon(mainWeapon);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetActiveWeapon(sideWeapon);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwapWeapon();
        }

        activeWeapon.WeaponUpdate();
    }

    public void SwapWeapon()
    {
        if (activeWeapon == mainWeapon)
        {
            SetActiveWeaponNetworked(sideWeapon);
        }
        else
        {
            SetActiveWeaponNetworked(mainWeapon);
        }
    }

    public void PickupWeapon(int weaponId)
    {
        Weapon weapon = allWeapons[weaponId];
        if (weapon == activeWeapon || weapon == mainWeapon || weapon == sideWeapon)
        {
            Logger.Default.LogError($"Weapon {weapon.name} [{weaponId}] already equipped!");
            return;
        }

        activeWeapon.SetEnabled(false);

        if (MainWeaponEquipped())
        {
            mainWeapon = weapon;
            SetActiveWeaponNetworked(mainWeapon);
        }
        else // SideWeaponEquipped()
        {
            sideWeapon = weapon;
            SetActiveWeaponNetworked(sideWeapon);
        }
    }

    #region SetActiveWeapon
    private void SetActiveWeaponNetworked(Weapon weapon)
    {
        SetActiveWeapon(weapon);

        int weaponIndex = GetWeaponIndex(weapon);
        if (weaponIndex != -1)
            SetActiveWeaponServerRpc(weaponIndex);
        else
            Logger.Default.LogError("Weapon not found!");
    }

    private void SetActiveWeapon(Weapon weapon)
    {
        if (activeWeapon == weapon) return;

        activeWeapon.SetEnabled(false);

        activeWeapon = weapon;
        OnActiveWeaponChanged?.Invoke(weapon);

        activeWeapon.SetEnabled(true);

        if (IsOwner)
        {
            player.HUD.UpdateWeapon(weapon.Name, weapon.Ammo, weapon.MaxAmmo);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetActiveWeaponServerRpc(int weaponIndex)
    {
        SetActiveWeaponClientRpc(weaponIndex);
    }

    [ClientRpc]
    public void SetActiveWeaponClientRpc(int weaponIndex)
    {
        if (!IsOwner)
            SetActiveWeapon(allWeapons[weaponIndex]);
    }
    #endregion

    public void SetEnabled(bool enabled)
    {
        crosshair.gameObject.SetActive(enabled);
        activeWeapon.SetEnabled(enabled);
    }

    public void SetEnableControls(bool enable)
    {
        scriptEnabled = enable;
    }

    public void ResetWeapons()
    {
        foreach (var weapon in allWeapons)
        {
            weapon.Reset();
        }

        mainWeapon = startingWeapons[0];
        sideWeapon = startingWeapons[1];
        SetActiveWeaponNetworked(mainWeapon);
    }

    private int GetWeaponIndex(Weapon weapon)
    {
        for (int i = 0; i < allWeapons.Length; i++)
        {
            if (allWeapons[i] == weapon)
            {
                return i;
            }
        }

        return -1;
    }


    public void MovementChanged(Vector3 velocity)
    {
        float percentage = velocity.magnitude / maxSpeedForMaxSpread;
        activeWeapon.CalculateMovementHipFireSpread(percentage);
    }

    private bool MainWeaponEquipped() => mainWeapon == activeWeapon;
    private bool SideWeaponEquipped() => sideWeapon == activeWeapon;

    public Weapon GetActiveWeapon() => activeWeapon;
    public string GetActiveWeaponName() => activeWeapon.Name;

    public Weapon GetWeapon(int weaponId) => allWeapons[weaponId];
    public string GetWeaponName(int weaponId) => allWeapons[weaponId].Name;
}
