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

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        if (!IsOwner) return;
        base.Start();
    }

    public override void WeaponUpdate()
    {
        if (!IsOwner) return;
        base.WeaponUpdate();
    }

    protected override void OnFire()
    {

    }
}
