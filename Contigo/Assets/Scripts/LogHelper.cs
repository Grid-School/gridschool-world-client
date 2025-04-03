using System;
using UnityEngine;

public static class LogHelper
{
    public static void Log(string message, UnityEngine.Object context = null)
    {
        Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] {message}", context);
    }

    public static void LogWarning(string message, UnityEngine.Object context = null)
    {
        Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] {message}", context);
    }

    public static void LogError(string message, UnityEngine.Object context = null)
    {
        Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] {message}", context);
    }
}