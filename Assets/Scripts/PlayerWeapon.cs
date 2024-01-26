using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] Pistol pistol;
    Crosshair crosshair;

    void Start()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        crosshair = FindObjectOfType<Crosshair>(true);
        crosshair.gameObject.SetActive(true);
    }

    public void SetEnabled(bool enabled)
    {
        crosshair.gameObject.SetActive(enabled);
        pistol.enabled = enabled;
    }
}
