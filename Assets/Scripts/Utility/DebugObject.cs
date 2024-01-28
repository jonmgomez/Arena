using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This script is used to destroy objects that are only used for debugging purposes.
/// An object with this script will be destroyed when the game is built in standalone.
/// </summary>
public class DebugObject : MonoBehaviour
{
    void Start()
    {
        #if !UNITY_EDITOR

        Destroy(this.gameObject);

        #endif
    }
}
