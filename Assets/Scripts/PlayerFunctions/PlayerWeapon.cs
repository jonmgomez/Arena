using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    Player player;
    [SerializeField] Weapon activeWeapon;
    [SerializeField] Weapon mainWeapon;
    [SerializeField] Weapon sideWeapon;
    [SerializeField] Weapon[] allWeapons;
    Crosshair crosshair;

    void Start()
    {
        // Disable all weapons except the active weapon
        // Includes all non-owner clients
        foreach (var weapon in allWeapons)
        {
            if (weapon != activeWeapon)
                weapon.SetEnabled(false);
            else
                weapon.SetEnabled(true);
        }

        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        player = GetComponent<Player>();
        player.HUD.UpdateWeapon(activeWeapon.Name, activeWeapon.Ammo, activeWeapon.MaxAmmo);
        activeWeapon.OnAmmoChanged += (ammo) => player.HUD.UpdateCurrentAmmo(ammo);

        crosshair = FindObjectOfType<Crosshair>(true);
        crosshair.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!IsOwner) return;

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

        activeWeapon.OnAmmoChanged -= (ammo) => player.HUD.UpdateCurrentAmmo(ammo);

        activeWeapon.SetEnabled(false);
        activeWeapon = weapon;
        activeWeapon.SetEnabled(true);

        if (IsOwner)
        {
            player.HUD.UpdateWeapon(weapon.Name, weapon.Ammo, weapon.MaxAmmo);
            activeWeapon.OnAmmoChanged += (ammo) => player.HUD.UpdateCurrentAmmo(ammo);
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
        // TODO: Do custom enabling of weapon script. Otherwise fire rate coroutines will be stopped.
        crosshair.gameObject.SetActive(enabled);
        mainWeapon.SetEnabled(enabled);
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

    private bool MainWeaponEquipped() => mainWeapon == activeWeapon;
    private bool SideWeaponEquipped() => sideWeapon == activeWeapon;
}
