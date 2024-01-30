using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pistol : Weapon
{
    [Header("Pistol")]
    Vector3 idlePosition;
    Vector3 recoilTargetPosition;
    Quaternion idleRotation;
    Quaternion recoilTargetRotation;

    protected override void Start()
    {
        if (!IsOwner) return;
        base.Start();

        idlePosition = transform.localPosition;
        idleRotation = transform.localRotation;
    }

    protected override void Update()
    {
        if (!IsOwner) return;
        base.Update();

        transform.localPosition = Vector3.Lerp(transform.localPosition, recoilTargetPosition, Time.deltaTime * 20f);
        recoilTargetPosition = Vector3.Lerp(recoilTargetPosition, idlePosition, Time.deltaTime * 10f);

        transform.localRotation = Quaternion.Lerp(transform.localRotation, recoilTargetRotation, Time.deltaTime * 20f);
        recoilTargetRotation = Quaternion.Lerp(recoilTargetRotation, idleRotation, Time.deltaTime * 10f);
    }

    protected override void OnFire()
    {
        recoilTargetPosition = idlePosition - firePoint.forward * Random.Range(0.1f, 0.2f);
        recoilTargetRotation = idleRotation * Quaternion.Euler(Random.Range(-25f, -10f), 0, 0f);
    }
}
