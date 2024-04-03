using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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
    AimFire,

    // Third Person Body Only
    TP_BODY_ANIMATIONS_START,
    TurnLeft,
    TurnRight,
    MoveIdle,
    MovementBlend
}

public class PlayerWeaponAnimator : NetworkBehaviour
{
    private readonly Logger logger = new("PLYRANIM");

    [Header("Player Animators")]
    [SerializeField] private Animator playerFirstPersonAnimator;
    [SerializeField] private Animator playerThirdPersonAnimator;

    [Header("Rig")]
    [SerializeField] private MultiAimConstraint thirdPersonHeadRig;
    [Tooltip("Speed at which the rig is restored to full visibility when the player is not aiming or reloading")]
    [SerializeField] private float rigRestoreSpeed = 2f;

    [Header("Stationary Turning")]
    [Tooltip("The maximum angle the player can turn before playing a turning animation")]
    [SerializeField] private float maxTurnAngleDegrees = 30f;

    [Header("References")]
    [SerializeField] Transform hipsBone;
    [SerializeField] Transform aimTarget;

    private Animator weaponAnimator;
    private Animator thirdPersonWeaponAnimator;

    private float currentRigsWeight = 1f;
    private bool rigVisible = true;

    private PlayerMovement playerMovement;
    private PlayerWeapon playerWeapon;
    private PlayerCamera playerCamera;

    private Coroutine animationEndCoroutine;
    private Coroutine animationCallbackCoroutine;
    private Coroutine turningCoroutine;

    private const float SEND_MOVEMENT_VALUES_INTERVAL = 0.33f;
    private float lastMovementValuesSent = 0f;

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
                // playerThirdPersonAnimator.CrossFade("MovementBlend", 0.25f);
                PlayAnimation(WeaponAnimation.MovementBlend);

                if (turningCoroutine != null)
                    StopCoroutine(turningCoroutine);
            }
            else
            {
                // playerThirdPersonAnimator.CrossFade("MoveIdle", 0.25f);
                PlayAnimation(WeaponAnimation.MoveIdle);
            }
        };

        playerCamera = GetComponentInChildren<PlayerCamera>();
    }

    void Update()
    {
        if (rigVisible && currentRigsWeight != 1f ||
            !rigVisible && currentRigsWeight != 0f)
        {
            float targetWeight = rigVisible ? 1f : 0f;
            currentRigsWeight = Mathf.MoveTowards(currentRigsWeight, targetWeight, Time.deltaTime * rigRestoreSpeed);
            thirdPersonHeadRig.weight = currentRigsWeight;
        }

        if (!IsOwner)
        {
            return;
        }

        if (playerThirdPersonAnimator != null)
        {
            if (playerMovement.IsMoving())
            {
                if (SEND_MOVEMENT_VALUES_INTERVAL - lastMovementValuesSent <= 0f)
                {
                    SetAnimatorFloatValue("Horizontal", Input.GetAxis("Horizontal"));
                    SetAnimatorFloatValue("Vertical", Input.GetAxis("Vertical"));
                    lastMovementValuesSent = 0f;
                }
                else
                {
                    playerThirdPersonAnimator.SetFloat("Horizontal", Input.GetAxis("Horizontal"));
                    playerThirdPersonAnimator.SetFloat("Vertical", Input.GetAxis("Vertical"));
                    lastMovementValuesSent += Time.deltaTime;
                }
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
            WeaponAnimation weaponAnimation = turnRight ? WeaponAnimation.TurnRight : WeaponAnimation.TurnLeft;
            PlayAnimation(weaponAnimation);

            string animation = AnimationEnumToString(weaponAnimation);
            float animationLength = GetAnimationLength(playerThirdPersonAnimator, animation);
            turningCoroutine = StartCoroutine(TurnPlayer(animationLength));
        }
    }

    IEnumerator TurnPlayer(float animationLength)
    {
        yield return new WaitForSeconds(animationLength);

        Vector3 newPlayerForward = new(hipsBone.forward.x, 0f, hipsBone.forward.z);
        RotatePlayerToNewForward(newPlayerForward);
        PlayAnimation(WeaponAnimation.MoveIdle);

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

    #region Play Animation
    /// <summary>
    /// Play an animation given a weapon animation enum
    /// </summary>
    /// <param name="animation">Weapon animation enum</param>
    public void PlayAnimation(WeaponAnimation animation)
    {
        if (animation == WeaponAnimation.TP_BODY_ANIMATIONS_START)
        {
            logger.LogError($"Enum value {animation} is not a valid animation!");
            return;
        }

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

        if (animation < WeaponAnimation.TP_BODY_ANIMATIONS_START) // Some animations such as TurnLeft only control the lower body and only need to be played on the third person body
        {
            PlayAnimationForController(weaponAnimator, weaponAnimation);
            PlayAnimationForController(playerFirstPersonAnimator, playerAnimation);
            PlayAnimationForController(thirdPersonWeaponAnimator, weaponAnimation);
            PlayAnimationForController(playerThirdPersonAnimator, playerAnimation);
        }
        else
        {
            // Animations exclusive to the third person body do not have the WeaponName_ prefix
            PlayAnimationForController(playerThirdPersonAnimator, weaponAnimation);
        }

        AnimationPlayedServerRpc(weaponAnimation);
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

    private void PlayNetworkedAnimation(WeaponAnimation animation)
    {
        if (weaponAnimator == null)
        {
            logger.LogError($"Weapon animator is null for {playerWeapon.GetActiveWeaponName()}! Animation: {animation}");
            return;
        }

        string weaponAnimation = AnimationEnumToString(animation);
        string weaponName = playerWeapon.GetActiveWeaponName();
        string playerAnimation = $"{weaponName}_{weaponAnimation}";

        OnAnimationStart(animation);

        if (animation < WeaponAnimation.TP_BODY_ANIMATIONS_START) // Some animations such as TurnLeft only control the lower body and only need to be played on the third person body
        {
            PlayAnimationForController(thirdPersonWeaponAnimator, weaponAnimation);
            PlayAnimationForController(playerThirdPersonAnimator, playerAnimation);
        }
        else
        {
            // Animations exclusive to the third person body do not have the WeaponName_ prefix
            PlayAnimationForController(playerThirdPersonAnimator, weaponAnimation);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AnimationPlayedServerRpc(FixedString128Bytes animation)
    {
        if (Net.IsServerOnly)
        {
            logger.Log($"animation: {animation}");
            PlayNetworkedAnimation((WeaponAnimation)Enum.Parse(typeof(WeaponAnimation), animation.ToString()));
        }

        AnimationPlayedClientRpc(animation);
    }

    [ClientRpc]
    private void AnimationPlayedClientRpc(FixedString128Bytes animations)
    {
        logger.Log($"animations: {animations}");
        logger.Log("Current weapon animator: " + weaponAnimator.name);

        if (!IsOwner)
        {
            PlayNetworkedAnimation((WeaponAnimation)Enum.Parse(typeof(WeaponAnimation), animations.ToString()));
        }
    }
    #endregion Play Animation

    #region Animation Parameters
    private void SetAnimatorFloatValue(string valueName, float value)
    {
        // This function assumes that the animator is the third person body animator for now
        playerThirdPersonAnimator.SetFloat(valueName, value);

        SetAnimatorFloatValueServerRpc(valueName, value);
    }

    private void SetNetworkedAnimatorFloatValue(string valueName, float value)
    {
        playerThirdPersonAnimator.SetFloat(valueName, value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetAnimatorFloatValueServerRpc(FixedString64Bytes valueName, float value)
    {
        if (Net.IsServerOnly)
        {
            SetAnimatorFloatValue(valueName.ToString(), value);
        }

        SetAnimatorFloatValueClientRpc(valueName, value);
    }

    [ClientRpc]
    private void SetAnimatorFloatValueClientRpc(FixedString64Bytes valueName, float value)
    {
        if (!IsOwner)
        {
            SetNetworkedAnimatorFloatValue(valueName.ToString(), value);
        }
    }
    #endregion Animation Parameters

    /// <summary>
    /// Get the length of an animation
    /// </summary>
    /// <param name="animator">Animator to search into</param>
    /// <param name="animation">Animation name to search for</param>
    /// <returns>float time of animation. 0 if non-existent.</returns>
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
