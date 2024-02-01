using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponEmptyState : WeaponState
{
    public WeaponEmptyState(Weapon weapon) : base(weapon)
    {
    }

    public override void Update()
    {
        CheckAim();

        bool reloadPressed = Input.GetKeyDown(KeyCode.R);
        if (reloadPressed || weapon.AttemptingFire)
        {
            weapon.SetState(new WeaponReloadingState(weapon));
        }
    }
}
