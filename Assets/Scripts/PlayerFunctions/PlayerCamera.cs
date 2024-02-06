using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    public readonly float xSensitivity = 2.5f;
    public readonly float ySensitivity = 2.5f;

    [SerializeField] Transform player;
    [SerializeField] Transform playerChildRotation;

    float xRotation;
    float yRotation;

    bool mouseFree = false;

    // Triggered when the camera is rotated with either x or y value != 0.
    public event Action<float, float> OnRotate;
    public event Action<Camera> OnCameraChanged;

    Camera cam;
    AudioListener audioListener;
    Camera defaultCamera;
    AudioListener defaultAudioListener;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            GetComponent<Camera>().enabled = false;
            GetComponent<AudioListener>().enabled = false;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cam = GetComponent<Camera>();
        cam.enabled = true;
        audioListener = GetComponent<AudioListener>();
        audioListener.enabled = true;

        defaultCamera = Camera.main;
        defaultCamera.enabled = false;
        defaultAudioListener = defaultCamera.GetComponent<AudioListener>();
        defaultAudioListener.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
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

        Rotate(mouseX, mouseY);
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        cam.enabled = enabled;
        audioListener.enabled = enabled;
        defaultCamera.enabled = !enabled;
        defaultAudioListener.enabled = !enabled;

        OnCameraChanged?.Invoke(enabled ? cam : defaultCamera);
    }

    /// <summary>
    /// Rotate the camera.
    /// <para>NOTE: X indicates the rotation around the Y axis (left and right), and Y indicates the rotation around the X axis (up and down).</para>
    /// </summary>
    /// <param name="x">Degrees to rotate left or right</param>
    /// <param name="y">Degrees to rotate up or down</param>
    public void Rotate(float x, float y, bool triggerEvent = true)
    {
        yRotation += x;
        xRotation -= y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, transform.localRotation.y, 0);
        player.rotation = Quaternion.Euler(0, yRotation, 0);
        playerChildRotation.rotation = Quaternion.Euler(xRotation, yRotation, 0);

        if ((x != 0 || y != 0) && triggerEvent)
            OnRotate?.Invoke(x, y);
    }

    public Camera GetCurrentCamera()
    {
        return cam.enabled ? cam : defaultCamera;
    }
}
