using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimDownSightsViewer : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    private float defaultFov;
    private Crosshair crosshair;

    [SerializeField] private Transform fpsArms;
    private Transform movingWeapon;

    private Vector3 desiredPosition;
    private float desiredFov;

    private readonly Dictionary<Transform, Vector3> originalPositions = new();

    [SerializeField] private float adsFovDelta = 10f;
    [SerializeField] private float adsSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool disableCrosshair = true;

    void Start()
    {
        defaultFov = mainCamera.fieldOfView;
        crosshair = FindObjectOfType<Crosshair>(true);
    }

    void Update()
    {
        if (movingWeapon != null)
        {
            Vector3 toArms = fpsArms.position - movingWeapon.position;
            movingWeapon.localPosition = Vector3.Lerp(movingWeapon.localPosition, desiredPosition, Time.deltaTime * adsSpeed);
            fpsArms.position = movingWeapon.position + toArms;

            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, desiredFov, Time.deltaTime * adsSpeed);

            if (Vector3.Distance(movingWeapon.position, desiredPosition) < 0.01f)
            {
                movingWeapon = null;
            }
        }
    }

    /// <summary>
    /// Positions the weapon and arms for aiming down sights
    /// </summary>
    /// <param name="weapon">Transform of the weapon to position</param>
    public void PositionObjects(Weapon weapon)
    {
        movingWeapon = weapon.transform;
        desiredPosition = weapon.GetAimPositionOffset();

        desiredFov = defaultFov - adsFovDelta;

        if (!originalPositions.ContainsKey(weapon.transform))
            originalPositions[weapon.transform] = weapon.transform.localPosition;

        if (disableCrosshair)
            crosshair.SetVisible(false);
    }

    /// <summary>
    /// Restores the weapon and arms to their original positions out of ADS
    /// </summary>
    /// <param name="weapon">Transform of the weapon to position</param>
    public void RestorePositions(Weapon weapon)
    {
        movingWeapon = weapon.transform;
        desiredPosition = originalPositions[weapon.transform];

        desiredFov = defaultFov;

        crosshair.SetVisible(true);
    }
}
