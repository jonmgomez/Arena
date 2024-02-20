using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogFile
{
    private static readonly string LOG_FILE = Application.dataPath + "/log.txt";

    public static void WriteToFile(object message)
    {
        WriteToFile(ObjectToString(message));
    }

    public static void WriteToFile(string message)
    {
        if (!System.IO.File.Exists(LOG_FILE))
        {
            System.IO.File.WriteAllText(LOG_FILE, "Log file created at " + DateTime.Now + Environment.NewLine);
        }

        System.IO.File.AppendAllText(LOG_FILE, message + Environment.NewLine);
    }

    private static string ObjectToString(object obj)
    {
        if (obj == null)
        {
            return "Null";
        }

        return obj.ToString();
    }
}

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
        message = FormatMessage(message);
        logger.Log(message);
        LogFile.WriteToFile($"INFO : {message}");
    }

    public void LogWarning(object message)
    {
        message = FormatMessage(message);
        logger.LogWarning("", FormatMessage(message));
        LogFile.WriteToFile($"WARNING : {message}");
    }

    public void LogError(object message)
    {
        message = FormatMessage(message);
        logger.LogError("", FormatMessage(message));
        LogFile.WriteToFile($"ERROR : {message}");
    }

    public void LogException(Exception exception)
    {
        string message = exception.Message;
        LogError($"Exception: {message}");
        LogFile.WriteToFile($"EXCEPTION : {message}");
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
