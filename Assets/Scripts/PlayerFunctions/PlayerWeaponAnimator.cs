using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public enum WeaponAnimation
{
    Idle,
    Ready,
    Fire,
    Reload,
    ReloadEmpty,
    ReloadStart,
    ReloadOne,
    ReloadEnd,
    AimIn,
    AimOut,
    AimIdle,
    AimFire
}

class PositionAndRotation
{
    public Vector3 position;
    public Quaternion rotation;

    public PositionAndRotation(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }
}

public class PlayerWeaponAnimator : NetworkBehaviour
{
    private readonly Logger logger = new("PLYRANIM");

    private Animator weaponAnimator;
    private Animator thirdPersonWeaponAnimator;
    [SerializeField] private Animator playerFirstPersonAnimator;
    [SerializeField] private Animator playerThirdPersonAnimator;

    [SerializeField] private MultiAimConstraint thirdPersonHeadRig;
    private float currentRigsWeight = 1f;
    private bool rigVisible = true;
    [SerializeField] private float rigRestoreSpeed = 2f;

    [SerializeField] private float maxTurnAngleDegrees = 15f;

    private PlayerMovement playerMovement;
    private PlayerWeapon playerWeapon;
    private PlayerCamera playerCamera;
    [SerializeField] Transform hipsBone;
    [SerializeField] Transform aimTarget;

    Coroutine animationEndCoroutine;
    Coroutine animationCallbackCoroutine;
    Coroutine turningCoroutine;

    void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        playerWeapon.OnActiveWeaponChanged += WeaponChanged;
        WeaponChanged(playerWeapon.GetActiveWeapon());
    }

    void Start()
    {
        if (!IsOwner) return;

        playerMovement = GetComponent<PlayerMovement>();
        playerMovement.OnMovementChange += (isMoving) =>
        {
            if (isMoving)
            {
                playerThirdPersonAnimator.CrossFade("MovementBlend", 0.25f);

                if (turningCoroutine != null)
                    StopCoroutine(turningCoroutine);
            }
            else
            {
                playerThirdPersonAnimator.CrossFade("MoveIdle", 0.25f);
            }
        };

        playerCamera = GetComponentInChildren<PlayerCamera>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (rigVisible && currentRigsWeight != 1f ||
            !rigVisible && currentRigsWeight != 0f)
        {
            float targetWeight = rigVisible ? 1f : 0f;
            currentRigsWeight = Mathf.MoveTowards(currentRigsWeight, targetWeight, Time.deltaTime * rigRestoreSpeed);
            thirdPersonHeadRig.weight = currentRigsWeight;
        }

        if (playerThirdPersonAnimator != null)
        {
            if (playerMovement.IsMoving())
            {
                playerThirdPersonAnimator.SetFloat("Horizontal", Input.GetAxis("Horizontal"));
                playerThirdPersonAnimator.SetFloat("Vertical", Input.GetAxis("Vertical"));
            }
            else
            {
                CheckForTurn();
            }
        }
    }

    private void CheckForTurn()
    {
        Vector3 rotation = playerCamera.transform.localEulerAngles;
        if (rotation.y > 180f)
        {
            rotation.y -= 360f;
        }

        bool turnRight = rotation.y > 0f;
        float difference = Mathf.Abs(rotation.y);

        if (difference > maxTurnAngleDegrees * 2.5f)
        {
            Vector3 newForward;
            if (turnRight)
            {
                newForward = Quaternion.Euler(0f, maxTurnAngleDegrees / 2, 0f).normalized * transform.forward;
            }
            else
            {
                newForward = Quaternion.Euler(0f, -maxTurnAngleDegrees / 2, 0f).normalized * transform.forward;
            }

            RotatePlayerToNewForward(newForward);
        }
        else if  (difference > maxTurnAngleDegrees && turningCoroutine == null)
        {
            string animation = turnRight ? "TurnRight" : "TurnLeft";
            PlayAnimationForController(playerThirdPersonAnimator, animation);

            float animationLength = GetAnimationLength(playerThirdPersonAnimator, animation);
            turningCoroutine = StartCoroutine(TurnPlayer(animationLength));
        }
    }

    IEnumerator TurnPlayer(float animationLength)
    {
        yield return new WaitForSeconds(animationLength);

        Vector3 newPlayerForward = new(hipsBone.forward.x, 0f, hipsBone.forward.z);
        RotatePlayerToNewForward(newPlayerForward);
        PlayAnimationForController(playerThirdPersonAnimator, "MoveIdle");

        turningCoroutine = null;
    }

    private void RotatePlayerToNewForward(Vector3 forward)
    {
        Vector3 cameraForward = new(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z);
        Vector3 aimTargetPosition = aimTarget.position;
        transform.rotation = Quaternion.LookRotation(forward);
        aimTarget.position = aimTargetPosition;

        // After the player root has been rotated, the camera is now offset based on its local rotation.
        // Adjust its rotation so that its forward lines up with where it used to be.
        Vector3 rotation = Quaternion.FromToRotation(transform.forward, cameraForward).eulerAngles;
        playerCamera.SetRotation(playerCamera.transform.localEulerAngles.x, rotation.y);
    }

    public void WeaponChanged(Weapon weapon)
    {
        weaponAnimator = weapon.Animator;
        if (weaponAnimator == null)
        {
            logger.LogError($"Weapon animator is null for {weapon.name}!");
            return;
        }
        thirdPersonWeaponAnimator = weapon.ThirdPersonWeaponAnimator;
    }

    private void PlayAnimationForController(Animator animator, string animation)
    {
        if (animator == null)
        {
            logger.LogError($"An animator is null for {playerWeapon.GetActiveWeaponName()}! Animation: {animation}");
            return;
        }

        #if UNITY_EDITOR
        bool found = false;
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, Animator.StringToHash(animation)))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            logger.LogError($"Animation {animation} not found in {animator.name}! Layers checked: (0 - {animator.layerCount})");
            return;
        }
        #endif

        animator.Play(animation, -1, 0f);
    }

    private string AnimationEnumToString(WeaponAnimation animation)
    {
        return animation.ToString();
    }

    /// <summary>
    /// Play an animation given a weapon animation enum
    /// </summary>
    /// <param name="animation">Weapon animation enum</param>
    public void PlayAnimation(WeaponAnimation animation)
    {
        if (animationEndCoroutine != null)
            StopCoroutine(animationEndCoroutine);

        if (animationCallbackCoroutine != null)
            StopCoroutine(animationCallbackCoroutine);

        if (weaponAnimator == null)
        {
            logger.LogError($"Weapon animator is null for {playerWeapon.GetActiveWeaponName()}! Animation: {animation}");
            return;
        }

        string weaponAnimation = AnimationEnumToString(animation);
        string weaponName = playerWeapon.GetActiveWeaponName();
        string playerAnimation = $"{weaponName}_{weaponAnimation}";

        OnAnimationStart(animation);

        PlayAnimationForController(weaponAnimator, weaponAnimation);
        PlayAnimationForController(thirdPersonWeaponAnimator, weaponAnimation);
        PlayAnimationForController(playerFirstPersonAnimator, playerAnimation);
        PlayAnimationForController(playerThirdPersonAnimator, playerAnimation);
    }

    /// <summary>
    /// Play an animation and call a function when it's finished
    /// </summary>
    /// <param name="animation">Weapon animation enum</param>
    /// <param name="OnFinished">Callback function</param>
    public void PlayAnimation(WeaponAnimation animation, Action OnFinished)
    {
        PlayAnimation(animation);

        float animationLength = GetAnimationLength(weaponAnimator, AnimationEnumToString(animation));
        animationLength = animationLength == 0f ? 0.01f : animationLength;

        animationEndCoroutine = StartCoroutine(AnimationCallback(animationLength, OnFinished));
    }

    private float GetAnimationLength(Animator animator, string animation)
    {
        if (animator == null) return 0f;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        foreach (AnimationClip clip in clips)
        {
            // Note that the clip name refers to the name of the animation file itself, not what it is referred to in the animator
            // So check if the clip ends with "Fire" or "Reload" etc.
            if (clip.name.EndsWith(animation))
            {
                return clip.length;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Play an animation and call a function when it's finished. Also call a secondary function after a certain time
    /// </summary>
    /// <param name="animation">Weapon animation enum</param>
    /// <param name="OnFinished">Finished callback function</param>
    /// <param name="callbackTime">Time after animation start to call secondary function</param>
    /// <param name="CallbackFunction">Secondary callback function</param>
    public void PlayAnimation(WeaponAnimation animation, Action OnFinished, float callbackTime, Action CallbackFunction)
    {
        PlayAnimation(animation, OnFinished);

        if (!HasAnimation(animation))
        {
            CallbackFunction();
        }
        else
        {
            animationCallbackCoroutine = StartCoroutine(AnimationCallback(callbackTime, CallbackFunction));
        }
    }

    IEnumerator AnimationCallback(float time, Action function)
    {
        yield return new WaitForSeconds(time);
        function();
    }

    private void OnAnimationStart(WeaponAnimation animation)
    {
        switch (animation)
        {
            case WeaponAnimation.Reload:
            case WeaponAnimation.ReloadEmpty:
            case WeaponAnimation.ReloadStart:
            case WeaponAnimation.ReloadOne:
            case WeaponAnimation.ReloadEnd:
                SetThirdPersonRigVisibility(false);
                break;
            default:
                SetThirdPersonRigVisibility(true);
                break;
        }
    }

    private void SetThirdPersonRigVisibility(bool visible)
    {
        rigVisible = visible;
    }

    public bool HasAnimation(WeaponAnimation animation)
    {
        if (weaponAnimator == null)
        {
            return false;
        }

        string weaponAnimation = AnimationEnumToString(animation);
        string playerAnimation = $"{playerWeapon.GetActiveWeaponName()}_{weaponAnimation}";

        return weaponAnimator.HasState(0, Animator.StringToHash(weaponAnimation)) ||
               thirdPersonWeaponAnimator.HasState(0, Animator.StringToHash(weaponAnimation)) ||
               playerFirstPersonAnimator.HasState(0, Animator.StringToHash(playerAnimation)) ||
               playerThirdPersonAnimator.HasState(0, Animator.StringToHash(playerAnimation));
    }

    public bool HasFirstPersonAnimation(WeaponAnimation animation)
    {
        if (weaponAnimator == null)
        {
            return false;
        }

        string weaponAnimation = AnimationEnumToString(animation);
        string playerAnimation = $"{playerWeapon.GetActiveWeaponName()}_{weaponAnimation}";

        return weaponAnimator.HasState(0, Animator.StringToHash(weaponAnimation)) ||
               playerFirstPersonAnimator.HasState(0, Animator.StringToHash(playerAnimation));
    }

    public bool HasThirdPersonAnimation(WeaponAnimation animation)
    {
        if (weaponAnimator == null)
        {
            return false;
        }

        string weaponAnimation = AnimationEnumToString(animation);
        string playerAnimation = $"{playerWeapon.GetActiveWeaponName()}_{weaponAnimation}";

        return thirdPersonWeaponAnimator.HasState(0, Animator.StringToHash(weaponAnimation)) ||
               playerThirdPersonAnimator.HasState(0, Animator.StringToHash(playerAnimation));
    }
}
