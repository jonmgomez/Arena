using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of active camera
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    public event System.Action<Camera> OnCameraChanged;
    private Camera activeCamera;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        activeCamera = Camera.main;
    }

    public Camera GetActiveCamera => activeCamera;

    public void SetActiveCamera(Camera camera)
    {
        activeCamera.enabled = false;
        activeCamera.GetComponent<AudioListener>().enabled = false;

        activeCamera = camera;
        activeCamera.enabled = true;
        activeCamera.GetComponent<AudioListener>().enabled = true;

        OnCameraChanged?.Invoke(activeCamera);
    }
}
