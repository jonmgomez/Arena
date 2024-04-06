using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeMenu : UIMenu
{
    [SerializeField] private GameObject menuObject;
    [SerializeField] private GameObject settingsObject;
    [SerializeField] private GameObject exitConfirmationObject;

    [Header("Debug")]
    [SerializeField] private bool showMenu = true;

    public void Start()
    {
        menuObject.SetActive(false);
        settingsObject.SetActive(false);
        exitConfirmationObject.SetActive(false);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TrySetMenuEnabled();
        }
    }

    public override void SetMenuEnabled(bool enabled)
    {
        if (!enabled)
        {
            SetSettingsEnabled(false);
            SetExitConfirmationEnabled(false);
        }

        if (showMenu)
            menuObject.SetActive(enabled);

        Player player = GameState.Instance.GetLocalPlayer();
        if (player != null)
        {
            player.SetEnableControls(!enabled);
        }
    }

    public void SetSettingsEnabled(bool enabled)
    {
        settingsObject.SetActive(enabled);
    }

    public void SetExitConfirmationEnabled(bool enabled)
    {
        exitConfirmationObject.SetActive(enabled);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
