using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    public readonly float xSenstivity = 2.5f;
    public readonly float ySenstivity = 2.5f;

    [SerializeField] Transform player;
    [SerializeField] Transform playerChildRotation;

    float xRotation;
    float yRotation;

    bool mouseFree = false;

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

        float mouseX = Input.GetAxisRaw("Mouse X") * xSenstivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * ySenstivity;

        Rotate(mouseX, mouseY);
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        cam.enabled = enabled;
        audioListener.enabled = enabled;
        defaultCamera.enabled = !enabled;
        defaultAudioListener.enabled = !enabled;
    }

    public void Rotate(float x, float y)
    {
        yRotation += x;
        xRotation -= y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        player.rotation = Quaternion.Euler(0, yRotation, 0);
        playerChildRotation.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }
}
