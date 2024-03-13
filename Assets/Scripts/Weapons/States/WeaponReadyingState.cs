using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponReadyingState : WeaponState
{
    public WeaponReadyingState(Weapon weapon) : base(weapon)
    {
    }

    public override State GetStateType()
    {
        return State.Readying;
    }

    public override bool ShouldEnter()
    {
        return true;
    }

    public override void OnStateEnter(State previousState)
    {
        weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.Ready, () => weapon.SetState(State.Ready));
    }
}
