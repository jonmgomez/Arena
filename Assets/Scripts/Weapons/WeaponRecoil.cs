using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    const float MAX_ROTATION = 360f;
    const float MAX_Y_ROTATION = 180f;

    Weapon weapon;
    PlayerCamera playerCamera;

    [Header("Base Recoil")]
    [Range(-3f, 0f)] [SerializeField] private float horizontalMinimum = -0.2f; // Recoil can only be negative x for left and right. Must be positive for up
    [Range(0f,  3f)] [SerializeField] private float horizontalMaximum = 0.4f;
    [Range(0f,  3f)] [SerializeField] private float verticalMinimum   = 0.2f;
    [Range(0f,  3f)] [SerializeField] private float verticalMaximum   = 0.4f;

    [Header("Recoil Recovery")]
    [SerializeField] private float cameraRecoilRecoverySpeed = 35;
    [SerializeField] private float cameraRecoilRecoveryDelay = 0.1f;
    [Tooltip("When the player adjusts for recoil and goes further than the added recoil, the camera will pull down this amount at minimum")]
    [SerializeField] private float minimumVerticalRecoveryDegrees = 0.05f;
    private bool recovering = false;
    Coroutine recoverCoroutine = null;
    Vector2 currentRecoilingOffset = Vector2.zero;

    [Header("Continuous Fire Weakens Recoil")]
    [SerializeField] private bool weakenRecoilOverTime = true;
    [SerializeField] private float horizontalContinuousFireMinimumScaler = 0.1f;
    [SerializeField] private float verticalContinuousFireMinimumScaler   = 0.1f;

    private int maxBullets = 0;
    private int bulletsFired = 0;
    private float bulletsTimer = 0;
    private float bulletsInterval = 0f;

    private void Start()
    {
        weapon = GetComponent<Weapon>();
        playerCamera = transform.root.GetComponentInChildren<PlayerCamera>();
        playerCamera.OnRotate += CameraRotated;

        bulletsInterval = weapon.GetFireRate() + 0.1f;
        maxBullets = weapon.MaxAmmo;
    }

    private void Update()
    {
        bulletsTimer -= Time.deltaTime;

        if (recovering)
        {
            Vector2 recoveryRotation = Vector2.one * (cameraRecoilRecoverySpeed * Time.deltaTime);

            recoveryRotation.x = Mathf.Clamp(recoveryRotation.x, 0f, Mathf.Abs(currentRecoilingOffset.x)) * Mathf.Sign(currentRecoilingOffset.x);
            recoveryRotation.y = -Mathf.Clamp(recoveryRotation.y, 0f, Mathf.Abs(currentRecoilingOffset.y)); // Should always be negative

            playerCamera.Rotate(recoveryRotation.x, recoveryRotation.y, false);

            if (recoveryRotation.x == currentRecoilingOffset.x && recoveryRotation.y == currentRecoilingOffset.y)
            {
                recovering = false;
                currentRecoilingOffset = Vector2.zero;
                recoveryRotation = Vector2.zero;
            }
            else
            {
                currentRecoilingOffset -= recoveryRotation;
            }
        }
    }

    private void CameraRotated(float x, float y)
    {
        if (currentRecoilingOffset.x == 0 && currentRecoilingOffset.y == 0)
            return;

        if (y < 0)
        {
            currentRecoilingOffset.y -= y;
            currentRecoilingOffset.y = Mathf.Clamp(currentRecoilingOffset.y, -MAX_Y_ROTATION, -minimumVerticalRecoveryDegrees);
        }
    }

    private void CountBulletsFired()
    {
        if (bulletsTimer > 0)
        {
            bulletsTimer -= Time.deltaTime;
        }
        else
        {
            // This weapon stopped firing for a while, reset the bulletsFired
            bulletsFired = 0;
        }
        bulletsFired++;
    }

    public void CalculateRecoil()
    {
        float recoilHorizontal = Random.Range(horizontalMinimum, horizontalMaximum);
        float recoilVertical = Random.Range(verticalMinimum, verticalMaximum);

        if (weakenRecoilOverTime)
        {
            CountBulletsFired();

            float percent = (float) bulletsFired / maxBullets;
            float horizontalScaler = 1f - (1f - horizontalContinuousFireMinimumScaler) * percent;
            float verticalScaler   = 1f - (1f - verticalContinuousFireMinimumScaler) * percent;

            recoilHorizontal *= horizontalScaler;
            recoilVertical *= verticalScaler;

            bulletsTimer = bulletsInterval;
        }

        Vector3 oldRotation = playerCamera.transform.rotation.eulerAngles;
        playerCamera.Rotate(recoilHorizontal, recoilVertical, false);
        Vector3 newRotation = playerCamera.transform.rotation.eulerAngles;

        // Account for the situation in which rotation wraps around 360 degrees
        if (recoilHorizontal > 0 && oldRotation.y > newRotation.y)
        {
            currentRecoilingOffset.x -= MAX_ROTATION - oldRotation.y + newRotation.y;
        }
        else if (recoilHorizontal < 0 && oldRotation.y < newRotation.y)
        {
            currentRecoilingOffset.x += MAX_ROTATION - newRotation.y + oldRotation.y;
        }
        else
        {
            currentRecoilingOffset.x -= newRotation.y - oldRotation.y;
        }

        // Unsure why this occurs, but sometimes when vertical recoil is 0, the measured rotation difference here is an epsilon value.
        // This at leasts prevents a 360 degree recoil from being applied when it occurs. Does not seem to happen on the x rotation either.
        // Strange behavior
        if (oldRotation.x < newRotation.x - Mathf.Epsilon)
        {
            currentRecoilingOffset.y -= MAX_ROTATION - newRotation.x + oldRotation.x;
        }
        else
        {
            currentRecoilingOffset.y -= oldRotation.x - newRotation.x;
        }

        recoverCoroutine = this.RestartCoroutine(RecoverFromRecoil(), recoverCoroutine);
    }

    private IEnumerator RecoverFromRecoil()
    {
        recovering = false;
        yield return new WaitForSeconds(cameraRecoilRecoveryDelay);
        recovering = true;
    }
}
