using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponReadyState : WeaponState
{
    const int RIGHT_MOUSE_BUTTON = 1;

    public WeaponReadyState(Weapon weapon) : base(weapon)
    {
    }

    public override bool ShouldEnter()
    {
        return true;
    }

    public override void OnStateEnter()
    {
        weapon.WeaponAnimator.PlayAnimation("Idle");
    }

    public override void Update()
    {
        CheckAim();

        if (!CheckReload()) // Make sure we have not transitioned to the reloading state
        {
            CheckFire();
        }
    }

    private bool CheckReload()
    {
        bool reloadPressed = Input.GetKeyDown(KeyCode.R);
        bool fullAmmo = weapon.Ammo >= weapon.MaxAmmo;

        if (!fullAmmo && reloadPressed)
        {
            weapon.SetState(State.Reloading);
            return true;
        }

        return false;
    }

    private void CheckFire()
    {
        if (weapon.AttemptingFire)
        {
            weapon.Ammo--;

            // This needs to be delegated to the weapon itself due to networked firing
            weapon.Fire();
            weapon.WeaponAnimator.PlayAnimation("Fire");

            if (!weapon.AimedIn)
            {
                CalculateBloom();
            }

            if (weapon.FireRate > 0)
            {
                weapon.SetState(State.Recovering);
            }
        }
    }

    private void CalculateBloom()
    {
        weapon.Crosshair.Bloom(weapon.BloomPerShotPercent);
        weapon.Bloom.AddBloom(weapon.BloomPerShotPercent);
    }
}
