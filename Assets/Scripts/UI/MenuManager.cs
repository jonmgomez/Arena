using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIMenu : MonoBehaviour
{
    public abstract void SetMenuEnabled(bool enabled);

    public void TrySetMenuEnabled()
    {
        MenuManager menuManager = MenuManager.Instance;

        if (menuManager != null)
        {
            menuManager.TrySetMenuEnabled(this);
        }
        else
        {
            Logger.Default.LogError("MenuManager not found");
        }
    }
}

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    private UIMenu currentMenu = null;
    private bool menuForceEnabled = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void TrySetMenuEnabled(UIMenu menu)
    {
        if (menuForceEnabled) // If the menu is forced enabled, don't allow any other menu to be enabled
           return;

        Debug.Assert(menu != null);
        if (currentMenu != null && currentMenu != menu)
        {
            currentMenu.SetMenuEnabled(false);
        }

        if (currentMenu == menu)
        {
            menu.SetMenuEnabled(false);
            currentMenu = null;
        }
        else
        {
            menu.SetMenuEnabled(true);
            currentMenu = menu;
        }
    }

    public void ForceMenuEnabled(UIMenu menu)
    {
        if (currentMenu != null)
        {
            currentMenu.SetMenuEnabled(false);
        }

        currentMenu = menu;
        if (currentMenu != null)
        {
            currentMenu.SetMenuEnabled(true);
        }

        menuForceEnabled = true;
    }

    public void RemoveForceMenuEnabled()
    {
        menuForceEnabled = false;
        if (currentMenu != null)
        {
            currentMenu.SetMenuEnabled(false);
            currentMenu = null;
        }
    }
}
