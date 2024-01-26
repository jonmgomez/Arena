using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button serverButton;
    [SerializeField] Button clientButton;
    [SerializeField] Button startButton;
    [SerializeField] GameObject clientWaitMessage;

    void Start()
    {
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            EnableServerInterface();
        });

        serverButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            EnableServerInterface();
        });

        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            EnableClientInterface();
        });

        startButton.onClick.AddListener(() => {
            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        });
    }

    private void EnableServerInterface()
    {
        hostButton.gameObject.SetActive(false);
        serverButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);

        startButton.gameObject.SetActive(true);
    }

    private void EnableClientInterface()
    {
        hostButton.gameObject.SetActive(false);
        serverButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);

        clientWaitMessage.SetActive(true);
    }
}
