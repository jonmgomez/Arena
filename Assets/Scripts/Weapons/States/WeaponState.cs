using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponState
{
    /// Sorted in order of which would be entered first given weapon's values/state
    public enum State
    {
        Readying,
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
    public abstract State GetStateType();

    public virtual void OnStateEnter(State previousState) { }
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
                weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.AimIdle);
                weapon.ADSViewer.PositionObjects(weapon.transform);
            }
            else if (weapon.AimedIn && !rmbDown)
            {
                weapon.AimedIn = false;
                weapon.WeaponAnimator.PlayAnimation(WeaponAnimation.Idle);
                weapon.ADSViewer.RestorePositions(weapon.transform);
            }
        }
    }
}
