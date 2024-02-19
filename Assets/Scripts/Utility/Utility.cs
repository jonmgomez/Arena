using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Profiling;
using UnityEditor.PackageManager;
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

    public static ClientRpcParams SendToOneClient(ulong clientId)
    {
        return CreateClientRpcParams(clientId);
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

    public static string ClientIdToString(ulong clientId)
    {
        return $"[{clientId}" + (clientId == NetworkManager.Singleton.LocalClientId ? " (Self)" : "") + "]";
    }

    public static string ClientNameToString(ulong clientId)
    {
        ClientData clientData = GameState.Instance.GetClientData(clientId);
        string name = " (" + clientData?.clientName + ")" ?? "";
        return $"[{clientId}{name}" + (clientId == NetworkManager.Singleton.LocalClientId ? " (Self)" : "") + "]";
    }

    public static void Loop(int times, Action function)
    {
        for (int i = 0; i < times; i++)
        {
            function();
        }
    }
}