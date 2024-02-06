using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponState
{
    /// Sorted in order of which would be entered first given weapon's values/state
    public enum State
    {
        Empty,
        Recovering,
        Reloading,
        Ready,
        Disabled
    }

    protected Weapon weapon;

    public WeaponState(Weapon weapon)
    {
        this.weapon = weapon;
    }

    public abstract bool ShouldEnter();

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
