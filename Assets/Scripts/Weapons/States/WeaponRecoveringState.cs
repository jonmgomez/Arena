using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponRecoveringState : WeaponState
{
    private float fireRecoveryTimeLeft = 0f;

    Coroutine autoReloadDelay;
    Coroutine recover;

    public WeaponRecoveringState(Weapon weapon) : base(weapon)
    {
    }

    public override State GetStateType()
    {
        return State.Recovering;
    }

    public override bool ShouldEnter()
    {
        return fireRecoveryTimeLeft > 0;
    }

    public override void OnStateEnter(State previousState)
    {
        if (fireRecoveryTimeLeft <= 0)
            fireRecoveryTimeLeft = weapon.FireRate;

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
            weapon.SetState(State.Reloading);
        }
    }

    IEnumerator AutoReloadDelay()
    {
        yield return new WaitForSeconds(weapon.AutoReloadDelay);

        if (weapon.Ammo <= 0)
        {
            weapon.SetState(State.Empty);
        }
    }

    IEnumerator Recover()
    {
        while (fireRecoveryTimeLeft > 0)
        {
            fireRecoveryTimeLeft -= Time.deltaTime;
            yield return null;
        }
        fireRecoveryTimeLeft = 0;

        if (weapon.Ammo <= 0)
        {
            weapon.SetState(State.Empty);
        }
        else
        {
            weapon.SetState(State.Ready);
        }
    }
}
