using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    const float MAX_ROTATION = 360f;
    const float MAX_Y_ROTATION = 90f;

    Weapon weapon;
    PlayerCamera playerCamera;
    PlayerMovement playerMovement;

    [Header("Base Recoil")]
    [Range(-3f, 0f)] [SerializeField] private float horizontalMinimum = -0.2f; // Recoil can only be negative x for left and right. Must be positive for up
    [Range(0f,  3f)] [SerializeField] private float horizontalMaximum = 0.4f;
    [Range(0f,  3f)] [SerializeField] private float verticalMinimum   = 0.2f;
    [Range(0f,  3f)] [SerializeField] private float verticalMaximum   = 0.4f;

    [Header("Recoil Recovery")]
    [Tooltip("The speed at which the camera recovers from recoil")]
    [SerializeField] private float cameraRecoilRecoverySpeed = 35f;
    [Tooltip("The delay before the camera starts to recover from recoil")]
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
        playerMovement = transform.root.GetComponent<PlayerMovement>();
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

            recoveryRotation.x = Mathf.Clamp(recoveryRotation.x, 0f, Mathf.Abs(currentRecoilingOffset.x)) * -Mathf.Sign(currentRecoilingOffset.x);
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
                currentRecoilingOffset += recoveryRotation;
            }
        }
    }

    private void CameraRotated(float horizontalRotation, float verticalRotation)
    {
        if (currentRecoilingOffset.x == 0 && currentRecoilingOffset.y == 0)
            return;

        if (verticalRotation < 0)
        {
            currentRecoilingOffset.y += verticalRotation;
            currentRecoilingOffset.y = Mathf.Clamp(currentRecoilingOffset.y, minimumVerticalRecoveryDegrees, MAX_Y_ROTATION);
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

            float percent = Mathf.Clamp01((float) bulletsFired / maxBullets);
            float horizontalScaler = 1f - (1f - horizontalContinuousFireMinimumScaler) * percent;
            float verticalScaler   = 1f - (1f - verticalContinuousFireMinimumScaler) * percent;

            recoilHorizontal *= horizontalScaler;
            recoilVertical *= verticalScaler;

            bulletsTimer = bulletsInterval;
        }

        playerCamera.Rotate(recoilHorizontal, recoilVertical, false);
        currentRecoilingOffset.x += recoilHorizontal;
        currentRecoilingOffset.y += recoilVertical;

        recoverCoroutine = this.RestartCoroutine(RecoverFromRecoil(), recoverCoroutine);
    }

    private IEnumerator RecoverFromRecoil()
    {
        recovering = false;
        yield return new WaitForSeconds(cameraRecoilRecoveryDelay);
        recovering = true;
    }
}
