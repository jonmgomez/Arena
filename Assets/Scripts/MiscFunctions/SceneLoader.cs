using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Scene
{
    // MENU SCENES
    MainMenu = -2147483647,
    GameSelect,

    // IN GAME SCENES
    Arena = 0,
    Prototype
}

public class SceneLoader : MonoBehaviour
{
    public static void LoadScene(Scene scene, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        string sceneName = GetSceneName(scene);
        LoadScene(sceneName, loadSceneMode);
    }

    public static void LoadScene(string scene, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        SceneManager.LoadScene(scene, loadSceneMode);
    }

    public static void LoadSceneNetworked(Scene scene, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        string sceneName = GetSceneName(scene);
        LoadSceneNetworked(sceneName, loadSceneMode);
    }

    public static void LoadSceneNetworked(string scene, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(scene, loadSceneMode);
    }

    private static string GetSceneName(Scene scene)
    {
        Debug.Log("Loading scene: " + scene);
        switch (scene)
        {
            case Scene.MainMenu:
                return "MainMenu";
            case Scene.GameSelect:
                return "GameSelect";
            case Scene.Arena:
                return "ArenaMain";
            case Scene.Prototype:
                return "Prototype";
            default:
                Logger.Default.LogError("Scene not found");
                return null;
        }
    }
}
