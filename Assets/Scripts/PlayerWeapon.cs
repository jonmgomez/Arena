using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] Weapon activeWeapon;
    [SerializeField] Weapon mainWeapon;
    [SerializeField] Weapon sideWeapon;
    [SerializeField] Weapon[] allWeapons;
    Crosshair crosshair;

    void Start()
    {
        foreach (var weapon in allWeapons)
        {
            if (weapon != activeWeapon)
            {
                weapon.gameObject.SetActive(false);
            }
        }

        if (!IsOwner)
        {
            enabled = false;
            return;
        }

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

    private void SetActiveWeaponNetworked(Weapon weapon)
    {
        SetActiveWeapon(weapon);

        int weaponIndex = GetWeaponIndex(weapon);
        if (weaponIndex != -1)
            SetActiveWeaponServerRpc(weaponIndex);
        else
            Debug.LogError("Weapon not found!");
    }

    private void SetActiveWeapon(Weapon weapon)
    {
        if (activeWeapon == weapon) return;

        activeWeapon.gameObject.SetActive(false);
        activeWeapon = weapon;
        activeWeapon.gameObject.SetActive(true);
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

    public void SetEnabled(bool enabled)
    {
        crosshair.gameObject.SetActive(enabled);
        mainWeapon.enabled = enabled;
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
}
