using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    public readonly float xSensitivity = 2.5f;
    public readonly float ySensitivity = 2.5f;

    [SerializeField] private Transform player;
    [SerializeField] private Transform aimTarget;

    private float xRotation;
    private float yRotation;

    private bool mouseFree = false;

    // Triggered when the camera is rotated with either x or y value != 0.
    public event Action<float, float> OnRotate;
    public event Action<float, float> OnRotationChange;

    private Camera playersCamera;
    private Camera defaultCamera;

    private PlayerMovement playerMovement;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            GetComponent<Camera>().enabled = false;
            GetComponent<AudioListener>().enabled = false;
        }
    }

    void Start()
    {
        if (!IsOwner)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playersCamera = GetComponent<Camera>();
        defaultCamera = Camera.main;
        CameraManager.Instance.SetActiveCamera(playersCamera);

        playerMovement = transform.root.GetComponent<PlayerMovement>();
        playerMovement.OnMovementChange += (isMoving) =>
        {
            // When the player is not moving the camera is allows to rotate freely left/right from the players root
            // rotation. (And will be adjusted if too far in PlayerWeaponAnimator.cs). When the player is moving the
            // camera will be locked to the players root rotation.
            if (isMoving)
            {
                SwapToPlayerRootRotation();
            }
            else
            {
                yRotation = 0f;
            }
        };
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            mouseFree = !mouseFree;
            Cursor.lockState = mouseFree ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = mouseFree;
        }

        if (mouseFree)
            return;

        float mouseX = Input.GetAxisRaw("Mouse X") * xSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * ySensitivity;

        Rotate(mouseX, mouseY, true);
    }

    void FixedUpdate()
    {
        // This is done on all clients in order to have the aimTarget position / spine rig aiming
        // synced across all clients.
        SetAimTarget();
    }

    private void SwapToPlayerRootRotation()
    {
        Vector3 forward = new(transform.forward.x, 0f, transform.forward.z);

        player.rotation = Quaternion.LookRotation(forward, Vector3.up);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        yRotation = player.rotation.eulerAngles.y;
    }

    private void SetAimTarget()
    {
        aimTarget.position = transform.position + transform.forward * 100f;
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        CameraManager.Instance.SetActiveCamera(enabled ? playersCamera : defaultCamera);
    }

    /// <summary>
    /// Rotate the camera.
    /// <para>NOTE: X indicates the rotation around the Y axis (left and right), and Y indicates the rotation around the X axis (up and down).</para>
    /// </summary>
    /// <param name="x">Degrees to rotate left or right</param>
    /// <param name="y">Degrees to rotate up or down</param>
    /// <param name="triggerEvent">Whether to trigger the OnRotate event</param>
    /// <param name="rotatePlayerRoot">Whether to rotate the player root instead of the camera pivot</param>
    public void Rotate(float x, float y, bool triggerEvent = true)
    {
        yRotation += x;
        xRotation -= y;
        xRotation.NormalizeRotationTo180();
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerMovement.IsMoving())
        {
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            player.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }

        if ((x != 0 || y != 0) && triggerEvent)
            OnRotate?.Invoke(x, y);
    }

    public void SetRotation(float x, float y)
    {
        xRotation = x;
        yRotation = y;
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        OnRotationChange?.Invoke(x, y);
    }
}
