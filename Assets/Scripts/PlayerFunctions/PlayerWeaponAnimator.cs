using System;
using System.Collections;
using System.Collections.Generic;
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

public class PlayerWeaponAnimator : MonoBehaviour
{
    private readonly Logger logger = new("PLYRANIM");

    private Animator weaponAnimator;
    private Animator thirdPersonWeaponAnimator;
    [SerializeField] private Animator playerFirstPersonAnimator;
    [SerializeField] private Animator playerThirdPersonAnimator;

    [SerializeField] private Transform thirdPersonHand;
    [SerializeField] private Transform thirdPersonParent;

    private Transform thirdPersonWeapon;
    [SerializeField] private MultiAimConstraint thirdPersonHandRig;
    [SerializeField] private MultiAimConstraint thirdPersonHeadRig;
    [SerializeField] private MultiAimConstraint thirdPersonChestRig;
    private float currentRigsWeight = 1f;
    private bool rigVisible = true;
    [SerializeField] private float rigRestoreSpeed = 2f;
    bool isMoving = false;

    [SerializeField] private float maxTurnAngleDegrees = 15f;

    private readonly Dictionary<Transform, PositionAndRotation> originalPositions = new();

    private PlayerMovement playerMovement;
    private PlayerWeapon playerWeapon;
    private PlayerCamera playerCamera;
    private float currentYRotation;
    [SerializeField] Transform hipsBone;
    [SerializeField] Transform spineBone;
    [SerializeField] Transform aimTarget;

    Coroutine turningCoroutine;

    void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
    }

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerMovement.OnMovementChange += (isMoving) =>
        {
            if (isMoving)
            {
                PlayAnimationForController(playerThirdPersonAnimator, "MovementBlend");

                if (turningCoroutine != null)
                    StopCoroutine(turningCoroutine);

                done = false;
            }
            else
            {
                PlayAnimationForController(playerThirdPersonAnimator, "MoveIdle");
                currentYRotation = 0f;
            }
        };

        playerCamera = GetComponentInChildren<PlayerCamera>();
        currentYRotation = playerCamera.transform.localRotation.eulerAngles.y;

        playerWeapon.OnActiveWeaponChanged += WeaponChanged;
        WeaponChanged(playerWeapon.GetActiveWeapon());
    }

    bool done = false;
    bool reenableRig = false;

    void Update()
    {
        if (reenableRig)
        {
            reenableRig = false;
            thirdPersonChestRig.weight = 1f;
        }

        float targetWeight = rigVisible ? 1f : 0f;

        if (rigVisible && currentRigsWeight != 1f ||
            !rigVisible && currentRigsWeight != 0f)
        {
            currentRigsWeight = Mathf.MoveTowards(currentRigsWeight, targetWeight, Time.deltaTime * rigRestoreSpeed);
            // thirdPersonHandRig.weight = currentRigsWeight;
            thirdPersonHeadRig.weight = currentRigsWeight;
            //thirdPersonChestRig.weight = currentRigsWeight;
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
        Vector3 rotation = playerCamera.transform.localRotation.eulerAngles;

        if (rotation.y > 180f)
        {
            rotation.y -= 360f;
        }

        if (Math.Abs(rotation.y - currentYRotation) > maxTurnAngleDegrees)
        {
            if (done)
            {
                return;
            }
            Debug.Log(rotation);
            done = true;

            currentYRotation = playerCamera.transform.localRotation.eulerAngles.y;
            // playerCamera.transform.localRotation = Quaternion.Euler(playerCamera.transform.localRotation.eulerAngles.x, 0f, playerCamera.transform.localRotation.eulerAngles.z);

            PlayAnimationForController(playerThirdPersonAnimator, "TurnLeft");

            float animationLength = 0f;
            AnimationClip[] clips = playerThirdPersonAnimator.runtimeAnimatorController.animationClips;

            foreach (AnimationClip clip in clips)
            {
                // Note that the clip name refers to the name of the animation file itself, not what it is referred to in the animator
                // So check if the clip ends with "Fire" or "Reload" etc.
                if (clip.name.EndsWith("TurnLeft"))
                {
                    animationLength = clip.length;
                    break;
                }
            }

            turningCoroutine = StartCoroutine(AnimationCallback(animationLength * 3f, () =>
            {


                // Vector3 playerForward = playerCamera.transform.forward;
                // Vector3 newForward = new(playerForward.x, 0f, playerForward.z);
                // Debug.DrawRay(playerCamera.transform.position, newForward * 10f, Color.red, 3f);

                Vector3 newForward = new(hipsBone.forward.x, 0f, hipsBone.forward.z);
                Debug.DrawRay(hipsBone.position, newForward * 10f, Color.blue, 3f);

                // playerCamera.transform.localRotation = Quaternion.Euler(playerCamera.transform.localRotation.eulerAngles.x, 0f, playerCamera.transform.localRotation.eulerAngles.z);

                // Get the difference in degrees between the transforms forward and the player camera forward

                Quaternion savedRotation = playerCamera.transform.rotation;
                Vector3 forward = playerCamera.transform.forward;
                Debug.DrawRay(playerCamera.transform.position, forward * 10f, Color.green, 3f);

                Vector3 aimTargetPosition = aimTarget.position;
                transform.rotation = Quaternion.LookRotation(newForward);
                aimTarget.position = aimTargetPosition;

                Vector3 cameraForward = new(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z);
                //playerCamera.transform.localRotation = Quaternion.LookRotation(newForward);
                Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 10f, Color.red, 3f);
                float angle = Vector3.SignedAngle(playerCamera.transform.forward, forward, Vector3.up);

                playerCamera.Rotate(angle, 0f);
                currentYRotation = playerCamera.transform.localRotation.eulerAngles.y;
                //playerCamera.transform.localRotation = Quaternion.LookRotation(forward);
                // playerCamera.xRotation = angle;
                Debug.Log(angle);

                // Use the forward vector to set the rotation of the this transform
                //transform.rotation = Quaternion.LookRotation(newForward);
                //Debug.Log(playerCamera.xRotation);
                //playerCamera.SetRotation(playerCamera.xRotation, 0f);

                playerThirdPersonAnimator.Play("MoveIdle", -1, 0f);
                // playerThirdPersonAnimator.CrossFade("MoveIdle", 0.1f, -1, 0.9f);

                //thirdPersonChestRig.weight = 0f;
                // Look toward aimTarget
                //spineBone.LookAt(playerCamera.transform.position + playerCamera.transform.forward * 10f, Vector3.up);

                //reenableRig = true;
            }));
        }
    }

    public void WeaponChanged(Weapon weapon)
    {
        weaponAnimator = weapon.Animator;
        thirdPersonWeapon = weapon.ThirdPersonWeapon.transform;
        thirdPersonWeaponAnimator = weapon.ThirdPersonWeaponAnimator;

        originalPositions[thirdPersonWeapon] = new PositionAndRotation(thirdPersonWeapon.localPosition, thirdPersonWeapon.localRotation);
    }

    private void PlayAnimationForController(Animator animator, string animation)
    {
        if (animator == null)
        {
            logger.LogError($"Animator is null for {playerWeapon.GetActiveWeaponName()}! Animation: {animation}");
            return;
        }

        #if UNITY_EDITOR
        if (!animator.HasState(0, Animator.StringToHash(animation)))
        {
            logger.LogError($"Animation {animation} not found in {animator.name}");
            //return;
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
        StopAllCoroutines();

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

        float animationLength = 0f;
        if (!HasAnimation(animation))
        {
            animationLength = 0.01f;
        }
        else
        {
            AnimationClip[] clips = weaponAnimator.runtimeAnimatorController.animationClips;

            foreach (AnimationClip clip in clips)
            {
                // Note that the clip name refers to the name of the animation file itself, not what it is referred to in the animator
                // So check if the clip ends with "Fire" or "Reload" etc.
                if (clip.name.EndsWith(AnimationEnumToString(animation)))
                {
                    animationLength = clip.length;
                    break;
                }
            }
        }

        StartCoroutine(AnimationCallback(animationLength, OnFinished));
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
            StartCoroutine(AnimationCallback(callbackTime, CallbackFunction));
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

        // if (visible)
        // {
        //     if (thirdPersonWeapon != null)
        //     {
        //         thirdPersonWeapon.parent = thirdPersonHand;

        //         if (originalPositions.ContainsKey(thirdPersonWeapon))
        //         {
        //             thirdPersonWeapon.SetLocalPositionAndRotation(originalPositions[thirdPersonWeapon].position, originalPositions[thirdPersonWeapon].rotation);
        //         }
        //     }
        // }
        // else
        // {
        //     if (thirdPersonWeapon != null)
        //         thirdPersonWeapon.parent = thirdPersonParent;
        // }
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
