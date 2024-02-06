using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = CameraManager.Instance.GetActiveCamera.transform;
        CameraManager.Instance.OnCameraChanged += (camera) => cam = camera.transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + cam.forward);
    }
}
