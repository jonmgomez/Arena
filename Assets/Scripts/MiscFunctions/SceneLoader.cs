using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Scene
{
    MainMenu,
    GameSelect,
    Arena
}

public class SceneLoader : MonoBehaviour
{
    public static void LoadScene(Scene scene, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        switch (scene)
        {
            case Scene.MainMenu:
                LoadScene("MainMenu", loadSceneMode);
                break;
            case Scene.GameSelect:
                LoadScene("GameSelect", loadSceneMode);
                break;
            case Scene.Arena:
                LoadScene("ArenaMain", loadSceneMode);
                break;
        }
    }

    public static void LoadScene(string scene, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        SceneManager.LoadScene(scene, loadSceneMode);
    }

    public static void LoadSceneNetworked(Scene scene, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        switch (scene)
        {
            case Scene.MainMenu:
                LoadSceneNetworked("MainMenu", loadSceneMode);
                break;
            case Scene.GameSelect:
                LoadSceneNetworked("GameSelect", loadSceneMode);
                break;
            case Scene.Arena:
                LoadSceneNetworked("ArenaMain", loadSceneMode);
                break;
        }
    }

    public static void LoadSceneNetworked(string scene, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(scene, loadSceneMode);
    }
}
