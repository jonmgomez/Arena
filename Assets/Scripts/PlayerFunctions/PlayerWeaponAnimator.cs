using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponAnimation
{
    Idle,
    Ready,
    Fire,
    Reload,
    ReloadEmpty,
    AimIn,
    AimOut
}

public class PlayerWeaponAnimator : MonoBehaviour
{
    private Animator weaponAnimator;
    [SerializeField] private Animator playerFirstPersonAnimator;
    [SerializeField] private Animator playerThirdPersonAnimator;

    private PlayerWeapon playerWeapon;

    public Event[] AnimationCallbacks;

    void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        weaponAnimator = playerWeapon.GetActiveWeapon().GetComponent<Animator>();
    }

    void Start()
    {
        playerWeapon.OnActiveWeaponChanged += WeaponChanged;
    }

    public void RegisterAnimationCallback()
    {

    }

    public void WeaponChanged(Weapon weapon)
    {
        weaponAnimator = weapon.GetComponent<Animator>();
        Debug.Log(weaponAnimator);
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

    private string AnimationEnumToString(WeaponAnimation animation)
    {
        return animation.ToString();
    }

    public void PlayAnimation(WeaponAnimation animation, Action OnFinished)
    {
        StopAllCoroutines();

        if (weaponAnimator == null)
        {
            Logger.Default.LogError("Weapon animator is null");
            return;
        }

        string weaponAnimation = AnimationEnumToString(animation);
        string weaponName = playerWeapon.GetActiveWeaponName();
        string playerAnimation = $"{weaponName}_{weaponAnimation}";

        PlayAnimationForController(weaponAnimator, weaponAnimation);
        PlayAnimationForController(playerFirstPersonAnimator, playerAnimation);
        PlayAnimationForController(playerThirdPersonAnimator, playerAnimation);

        AnimationClip[] clips = weaponAnimator.runtimeAnimatorController.animationClips;
        float animationLength = 0f;

        foreach (AnimationClip clip in clips)
        {
            // Note that the clip name refers to the name of the animation file itself, not what it is referred to in the animator
            // So check if the clip ends with "Fire" or "Reload" etc.
            if (clip.name.EndsWith(weaponAnimation))
            {
                animationLength = clip.length;
                break;
            }
        }

        StartCoroutine(AnimationCallback(animationLength, OnFinished));
    }

    public void PlayAnimation(WeaponAnimation animation, Action OnFinished, float callbackTime, Action CallbackFunction)
    {
        PlayAnimation(animation, OnFinished);

        StartCoroutine(AnimationCallback(callbackTime, CallbackFunction));
    }

    IEnumerator AnimationCallback(float time, Action function)
    {
        yield return new WaitForSeconds(time);
        function();
    }

    public bool HasAnimation(WeaponAnimation animation)
    {
        string weaponAnimation = AnimationEnumToString(animation);
        string playerAnimation = $"{playerWeapon.GetActiveWeaponName()}_{weaponAnimation}";

        return weaponAnimator.HasState(0, Animator.StringToHash(weaponAnimation)) ||
               playerFirstPersonAnimator.HasState(0, Animator.StringToHash(playerAnimation)) ||
               playerThirdPersonAnimator.HasState(0, Animator.StringToHash(playerAnimation));
    }
}
