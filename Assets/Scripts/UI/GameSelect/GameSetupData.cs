using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSetupData : MonoBehaviour
{
    [NonSerialized] public GameMode GameMode;
    [NonSerialized] public float TimeLimit;
    [NonSerialized] public int ScoreLimit;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
