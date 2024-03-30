using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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
        string name = !string.IsNullOrEmpty(clientData.clientName) ? " (" + clientData?.clientName + ")" : "";
        return $"[{clientId}{name}" + (clientId == NetworkManager.Singleton.LocalClientId ? " (Self)" : "") + "]";
    }

    public static string PlayerNameToString(ulong clientId)
    {
        ClientData clientData = GameState.Instance.GetClientData(clientId);
        string name = !string.IsNullOrEmpty(clientData.clientName) ? " (" + clientData?.clientName + ")" : "";
        return $"Player-[{clientId}{name}" + (clientId == NetworkManager.Singleton.LocalClientId ? " (Self)" : "") + "]";
    }

    public static void Loop(int times, Action function)
    {
        for (int i = 0; i < times; i++)
        {
            function();
        }
    }

    /// <summary>
    /// Normalize a rotation value around -180 to 180 degrees.
    /// <para>e.g. 270 degrees becomes -90 degrees.</para>
    /// </summary>
    public static void NormalizeRotationTo180(this ref float rotation)
    {
        rotation %= 360f;
        if (rotation < -180f)
            rotation += 360f;
        else if (rotation > 180f)
            rotation -= 360f;
    }

    /// <summary>
    /// Normalize a rotation vector2 to -180 to 180 degrees.
    /// </summary>
    /// <param name="rotation"></param>
    public static void NormalizeRotationTo180(this ref Vector2 rotation)
    {
        NormalizeRotationTo180(ref rotation.x);
        NormalizeRotationTo180(ref rotation.y);
    }

    /// <summary>
    /// Normalize a rotation vector3 to -180 to 180 degrees.
    /// </summary>
    /// <param name="rotation"></param>
    public static void NormalizeRotationTo180(this ref Vector3 rotation)
    {
        NormalizeRotationTo180(ref rotation.x);
        NormalizeRotationTo180(ref rotation.y);
        NormalizeRotationTo180(ref rotation.z);
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