using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Profiling;
using UnityEditor.PackageManager;
using UnityEngine;

public static class Utility
{
    /// MonoBehaviour Extensions
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
    /// MonoBehaviour Extensions

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
        string name = string.IsNullOrEmpty(clientData.clientName) ? " (" + clientData?.clientName + ")" : "";
        return $"[{clientId}{name}" + (clientId == NetworkManager.Singleton.LocalClientId ? " (Self)" : "") + "]";
    }

    public static void Loop(int times, Action function)
    {
        for (int i = 0; i < times; i++)
        {
            function();
        }
    }

    // Drawing Gizmos
    public static void DrawCube(Vector3 position, Vector3 size, Color color, float duration)
    {
        Vector3 halfSize = size / 2;

        Vector3[] vertices = new Vector3[]
        {
            position + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
            position + new Vector3( halfSize.x, -halfSize.y, -halfSize.z),
            position + new Vector3( halfSize.x, -halfSize.y,  halfSize.z),
            position + new Vector3(-halfSize.x, -halfSize.y,  halfSize.z),
            position + new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),
            position + new Vector3( halfSize.x,  halfSize.y, -halfSize.z),
            position + new Vector3( halfSize.x,  halfSize.y,  halfSize.z),
            position + new Vector3(-halfSize.x,  halfSize.y,  halfSize.z)
        };

        Debug.DrawLine(vertices[0], vertices[1], color, duration);
        Debug.DrawLine(vertices[1], vertices[2], color, duration);
        Debug.DrawLine(vertices[2], vertices[3], color, duration);
        Debug.DrawLine(vertices[3], vertices[0], color, duration);

        Debug.DrawLine(vertices[4], vertices[5], color, duration);
        Debug.DrawLine(vertices[5], vertices[6], color, duration);
        Debug.DrawLine(vertices[6], vertices[7], color, duration);
        Debug.DrawLine(vertices[7], vertices[4], color, duration);

        Debug.DrawLine(vertices[0], vertices[4], color, duration);
        Debug.DrawLine(vertices[1], vertices[5], color, duration);
        Debug.DrawLine(vertices[2], vertices[6], color, duration);
        Debug.DrawLine(vertices[3], vertices[7], color, duration);
    }
}