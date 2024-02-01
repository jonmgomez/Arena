using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponReloadingState : WeaponState
{
    public WeaponReloadingState(Weapon weapon) : base(weapon)
    {
    }

    public override void OnStateEnter()
    {
        Debug.Log("Reloading...");
        weapon.StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        yield return new WaitForSeconds(weapon.ReloadTime);
        weapon.Ammo = weapon.MaxAmmo;
        weapon.SetState(new WeaponReadyState(weapon));
    }
}
