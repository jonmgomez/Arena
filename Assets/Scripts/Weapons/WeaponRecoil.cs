using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    Weapon weapon;
    PlayerCamera playerCamera;

    [Range(-3f, 0f)] [SerializeField] private float horizontalMinimum = -0.2f;
    [Range(0f,  3f)] [SerializeField] private float horizontalMaximum = 0.4f;
    [Range(0f,  3f)] [SerializeField] private float verticalMinimum   = 0.2f;
    [Range(0f,  3f)] [SerializeField] private float verticalMaximum   = 0.4f;

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

        bulletsInterval = weapon.GetFireRate() + 0.1f;
        maxBullets = weapon.MaxAmmo;
    }

    private void Update()
    {
        bulletsTimer -= Time.deltaTime;
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
        }

        playerCamera.Rotate(recoilHorizontal, recoilVertical);

        bulletsTimer = bulletsInterval;
        // StartCoroutine(RecoverFromRecoil());
    }
}
