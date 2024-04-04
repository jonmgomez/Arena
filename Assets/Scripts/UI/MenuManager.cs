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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    public void TrySetMenuEnabled(UIMenu menu)
    {
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
}
