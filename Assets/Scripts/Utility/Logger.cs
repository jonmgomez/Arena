using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger
{
    private class DefaultLogHandler : ILogHandler
    {
        private readonly ILogHandler defaultLogHandler = Debug.unityLogger.logHandler;

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            defaultLogHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            defaultLogHandler.LogException(exception, context);
        }
    }

    public static Logger Default = new("");

    private readonly UnityEngine.Logger logger = new(new DefaultLogHandler());
    private readonly string region;
    private bool logDebug = false;

    public Logger(string region)
    {
        this.region = region;

        // Enable debug logging in the editor but not release builds
        #if UNITY_EDITOR
        logDebug = true;
        #endif
    }

    public void LogDebug(object message)
    {
        if (logDebug)
            Log(message);
    }

    public void Log(object message)
    {
        logger.Log(FormatMessage(message));
    }

    public void LogWarning(object message)
    {
        logger.LogWarning("", FormatMessage(message));
    }

    public void LogError(object message)
    {
        logger.LogError("", FormatMessage(message));
    }

    public void LogException(Exception exception)
    {
        string message = exception.Message;
        LogError($"Exception: {message}");
    }

    private object FormatMessage(object message)
    {
        if (string.IsNullOrEmpty(region))
            return message;

        return $"[{region}] {message}";
    }

    public void EnableDebugLogging()
    {
        logDebug = true;
    }

    public void DisableDebugLogging()
    {
        logDebug = false;
    }
}
