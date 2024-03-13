using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDisabledState : WeaponState
{
    public WeaponDisabledState(Weapon weapon) : base(weapon)
    {
    }

    public override State GetStateType()
    {
        return State.Disabled;
    }

    public override bool ShouldEnter()
    {
        return false;
    }
}
