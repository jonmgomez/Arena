using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponReloadingState : WeaponState
{
    private bool exiting = false;

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
        exiting = false;

        // If we were aiming in, restore the camera to its original position
        if (weapon.AimedIn)
        {
            weapon.AimedIn = false;
            weapon.ADSViewer.RestorePositions(weapon);
        }

        // Weapons can reload in two manners, full magazine reload or singular bullets like a shotgun
        if (weapon.ReloadSingles)
        {
            StartSinglesReload();
        }
        else if (weapon.Ammo <= 0 && weapon.WeaponAnimator.HasAnimation(WeaponAnimation.ReloadEmpty))
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

    public override void Update()
    {
        if (weapon.AttemptingFire)
        {
            if (weapon.ReloadSingles && !exiting)
            {
                void ToReady() => weapon.SetState(State.Ready);
                void ToEmpty() => weapon.SetState(State.Empty);
                weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.ReloadEnd, weapon.Ammo > 0 ? ToReady : ToEmpty);
                exiting = true;
            }
        }
    }

    private void StartSinglesReload()
    {
        weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.ReloadStart, SingleReloadLoop);
    }

    private void SingleReloadLoop()
    {
        if (weapon.Ammo >= weapon.MaxAmmo)
        {
            weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.ReloadEnd, Reloaded);
            exiting = true;
        }
        else
        {
            weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.ReloadOne,
                                                SingleReloadLoop,
                                                weapon.ReloadTime, () => { weapon.Ammo += weapon.ReloadSinglesAmount; });
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
