using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponReloadingState : WeaponState
{
    Coroutine reload;

    public WeaponReloadingState(Weapon weapon) : base(weapon)
    {
    }

    public override bool ShouldEnter()
    {
        return false;
    }

    public override void OnStateEnter()
    {
        reload = weapon.StartCoroutine(Reload());

        if (weapon.Ammo <= 0)
        {
            weapon.WeaponAnimator.PlayAnimation("ReloadEmpty");
        }
        else
        {
            weapon.WeaponAnimator.PlayAnimation("Reload");
        }
    }

    public override void OnStateExit()
    {
        weapon.StopCoroutine(reload);
    }

    IEnumerator Reload()
    {
        yield return new WaitForSeconds(weapon.ReloadTime);
        weapon.Ammo = weapon.MaxAmmo;
        weapon.SetState(State.Ready);
    }
}
