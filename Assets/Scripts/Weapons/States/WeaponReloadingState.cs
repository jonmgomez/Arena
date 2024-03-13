using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponReloadingState : WeaponState
{
    Coroutine reload;

    public WeaponReloadingState(Weapon weapon) : base(weapon)
    {
    }

    public override State GetStateType()
    {
        return State.Reloading;
    }

    public override bool ShouldEnter()
    {
        return false;
    }

    public override void OnStateEnter(State previousState)
    {
        if (weapon.Ammo <= 0 && weapon.WeaponAnimator.HasAnimation(WeaponAnimation.ReloadEmpty))
        {
            weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.ReloadEmpty,
                                                Reloaded,
                                                weapon.EmptyReloadTime, ResetAmmo);
        }
        else
        {
            weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.Reload,
                                                Reloaded,
                                                weapon.ReloadTime, ResetAmmo);
        }
    }

    private void ResetAmmo()
    {
        weapon.Ammo = weapon.MaxAmmo;
    }

    private void Reloaded()
    {
        weapon.SetState(State.Ready);
    }
}
