using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    // Start is called before the first frame update
    void Start()
    {
        List<Player> players = GameState.Instance.GetActivePlayers();
        foreach (Player player in players)
        {
            if (player.IsOwner)
            {
                PlayerCamera playerCamera = player.GetComponent<PlayerCamera>();
                cam = playerCamera.GetCurrentCamera().transform;
                playerCamera.OnCameraChanged += (camera) => cam = camera.transform;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + cam.forward);
    }
}
