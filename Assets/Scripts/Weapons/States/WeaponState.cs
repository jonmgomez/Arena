using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponState
{
    protected Weapon weapon;

    public WeaponState(Weapon weapon)
    {
        this.weapon = weapon;
    }

    public virtual void OnStateEnter() { }
    public virtual void OnStateExit() { }
    public virtual void Update() { }

    public void CheckAim()
    {
        if (weapon.CanADS)
        {
            bool rmbDown = Input.GetMouseButton(1);
            if (!weapon.AimedIn && rmbDown)
            {
                weapon.AimedIn = true;
            }
            else if (weapon.AimedIn && !rmbDown)
            {
                weapon.AimedIn = false;
            }
        }
    }
}
