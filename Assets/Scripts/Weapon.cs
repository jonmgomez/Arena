using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Profiling;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    const int LEFT_MOUSE_BUTTON = 0;
    const int RIGHT_MOUSE_BUTTON = 1;

    [Header("General")]
    [SerializeField] float damage = 10f;
    [SerializeField] bool canADS = false;
    bool aimedIn = false;
    [SerializeField] float fireRate = 0.1f;
    bool canFire = true;
    [SerializeField] bool isAutomatic = false;

    [SerializeField] float bloomPerShotPercent = 0.1f;
    [SerializeField] float recoilVerticalAmount = 0.1f;
    [SerializeField] float recoilRecoveryRate = 0.1f;
    [SerializeField] float recoilHorizontalAmount = 0.1f;

    protected abstract void OnFire();

    protected virtual void Start()
    {
        if (!IsOwner) return;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (!IsOwner) return;

        CheckAim();

        if (CheckFire())
        {
            StartCoroutine(FireRateCooldown());
            OnFire();
        }
    }

    private void CheckAim()
    {
        if (!canADS) return;

        if (Input.GetMouseButtonDown(RIGHT_MOUSE_BUTTON))
        {
            aimedIn = true;
        }
        else if (Input.GetMouseButtonUp(RIGHT_MOUSE_BUTTON))
        {
            aimedIn = false;
        }
    }

    private bool CheckFire()
    {
        if (!canFire) return false;

        if (isAutomatic && Input.GetMouseButton(LEFT_MOUSE_BUTTON))
        {
            return true;
        }
        else if (!isAutomatic && Input.GetMouseButtonDown(LEFT_MOUSE_BUTTON))
        {
            return true;
        }

        return false;
    }

    public bool IsAimedIn() => aimedIn;
    public float GetDamage() => damage;
    public float GetBloom() => bloomPerShotPercent;
    public (float horizontal, float vertical) GetRecoil() => (horizontal: recoilVerticalAmount, vertical: recoilHorizontalAmount);

    IEnumerator FireRateCooldown()
    {
        canFire = false;
        yield return new WaitForSeconds(fireRate);
        canFire = true;
    }
}
