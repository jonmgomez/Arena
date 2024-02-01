using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI weaponNameText;
    [SerializeField] TextMeshProUGUI ammoText;
    [SerializeField] TextMeshProUGUI maxAmmoText;

    public void UpdateCurrentAmmo(int ammo)
    {
        ammoText.text = ammo.ToString();
    }

    public void UpdateWeapon(string weaponName, int ammo, int maxAmmo)
    {
        weaponNameText.text = weaponName;
        ammoText.text = ammo.ToString();
        maxAmmoText.text = maxAmmo.ToString();
    }
}
