using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Logger
{
    public static void Log(object message)
    {
        Debug.Log(message);
    }

    public static void Log(object message, UnityEngine.Object context)
    {
        Debug.Log(message, context);
    }

    public static void LogWarning(object message)
    {
        Debug.LogWarning(message);
    }

    public static void LogWarning(object message, UnityEngine.Object context)
    {
        Debug.LogWarning(message, context);
    }

    public static void LogError(object message)
    {
        Debug.LogError(message);
    }

    public static void LogError(object message, UnityEngine.Object context)
    {
        Debug.LogError(message, context);
    }

        public static void LogException(Exception exception)
    {
        Debug.LogException(exception);
    }

    public static void LogException(Exception exception, UnityEngine.Object context)
    {
        Debug.LogException(exception, context);
    }
}
