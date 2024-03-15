using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimDownSightsViewer : MonoBehaviour
{
    [SerializeField] private Transform fpsArms;
    private readonly Dictionary<Transform, Vector3> originalPositions = new();

    public void PositionObjects(Transform weapon)
    {
        Vector3 desiredPosition = transform.position;

        if (!originalPositions.ContainsKey(weapon))
            originalPositions[weapon] = weapon.localPosition;
Debug.Log(weapon.localPosition);
        Vector3 toArms = fpsArms.position - weapon.position;
        weapon.position = desiredPosition;
Debug.Log(weapon.localPosition);
        fpsArms.position = desiredPosition + toArms;
    }

    public void RestorePositions(Transform weapon)
    {
        Vector3 toArms = fpsArms.position - weapon.position;
        weapon.localPosition = originalPositions[weapon];
        Debug.Log(weapon.localPosition);
        fpsArms.position = weapon.position + toArms;
    }
}
