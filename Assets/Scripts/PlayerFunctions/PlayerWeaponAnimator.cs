using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponAnimator : MonoBehaviour
{
    private Animator weaponAnimator;
    [SerializeField] private Animator playerFirstPersonAnimator;
    [SerializeField] private Animator playerThirdPersonAnimator;

    private PlayerWeapon playerWeapon;

    void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        weaponAnimator = playerWeapon.GetActiveWeapon().GetComponent<Animator>();
    }

    void Start()
    {
        playerWeapon.OnActiveWeaponChanged += WeaponChanged;
    }

    public void WeaponChanged(Weapon weapon)
    {
        weaponAnimator = weapon.GetComponent<Animator>();
    }

    private void PlayAnimationForController(Animator animator, string animation)
    {
        #if UNITY_EDITOR
        if (!animator.HasState(0, Animator.StringToHash(animation)))
        {
            Logger.Default.LogError($"Animation {animation} not found in {animator.name}");
            return;
        }
        #endif

        animator.Play(animation, -1, 0f);
    }

    public void PlayAnimation(string weaponAnimation)
    {
        string weaponName = playerWeapon.GetActiveWeaponName();
        string playerAnimation = $"{weaponName}_{weaponAnimation}";

        PlayAnimationForController(weaponAnimator, weaponAnimation);
        PlayAnimationForController(playerFirstPersonAnimator, playerAnimation);
        PlayAnimationForController(playerThirdPersonAnimator, playerAnimation);
    }
}
