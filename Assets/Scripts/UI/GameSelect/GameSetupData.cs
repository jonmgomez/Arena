using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSetupData : MonoBehaviour
{
    public GameMode GameMode;
    public float TimeLimit;
    public int ScoreLimit;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
