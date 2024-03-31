using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI weaponNameText;
    [SerializeField] TextMeshProUGUI ammoText;

    int currentAmmo;
    int maxAmmo;

    public void UpdateCurrentAmmo(int ammo)
    {
        Debug.Log("Updated ammo: " + ammo);
        currentAmmo = ammo;
        ammoText.text = GetAmmoString();
    }

    public void UpdateWeapon(string weaponName, int ammo, int maxAmmo)
    {
        currentAmmo = ammo;
        this.maxAmmo = maxAmmo;

        weaponNameText.text = weaponName;
        ammoText.text = GetAmmoString();

        Debug.Log("Updated weapon: " + weaponName + " with ammo: " + ammo + " / " + maxAmmo);
    }

    private string GetAmmoString()
    {
        return currentAmmo + " / " + maxAmmo;
    }
}
