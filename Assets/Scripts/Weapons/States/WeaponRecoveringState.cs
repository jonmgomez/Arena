using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponRecoveringState : WeaponState
{
    Coroutine autoReloadDelay;
    Coroutine recover;

    public WeaponRecoveringState(Weapon weapon) : base(weapon)
    {
    }

    public override void OnStateEnter()
    {
        autoReloadDelay = weapon.StartCoroutine(AutoReloadDelay());
        recover = weapon.StartCoroutine(Recover());
    }

    public override void OnStateExit()
    {
        weapon.StopCoroutine(autoReloadDelay);
        weapon.StopCoroutine(recover);
    }

    public override void Update()
    {
        CheckAim();

        if (Input.GetKeyDown(KeyCode.R))
        {
            weapon.SetState(new WeaponReloadingState(weapon));
        }
    }

    IEnumerator AutoReloadDelay()
    {
        yield return new WaitForSeconds(weapon.AutoReloadDelay);

        if (weapon.Ammo <= 0)
        {
            weapon.SetState(new WeaponEmptyState(weapon));
        }
    }

    IEnumerator Recover()
    {
        yield return new WaitForSeconds(weapon.FireRate);

        if (weapon.Ammo <= 0)
        {
            weapon.SetState(new WeaponEmptyState(weapon));
        }
        else
        {
            weapon.SetState(new WeaponReadyState(weapon));
        }
    }
}
