using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class Utility
{
    public static void Invoke(this MonoBehaviour mb, Action f, float delay)
    {
        mb.StartCoroutine(InvokeRoutine(f, delay));
    }

    private static IEnumerator InvokeRoutine(System.Action f, float delay)
    {
        yield return new WaitForSeconds(delay);
        f();
    }

    public static Coroutine RestartCoroutine(this MonoBehaviour mb, IEnumerator coroutine, Coroutine runningCoroutine)
    {
        if (runningCoroutine != null)
            mb.StopCoroutine(runningCoroutine);
        return mb.StartCoroutine(coroutine);
    }

    public static ClientRpcParams CreateClientRpcParams(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
    }
}