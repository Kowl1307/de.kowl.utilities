using Packages.de.kowl.utilities.Runtime.Kowl.Utils.UntyThreading;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable

public class UnityMainThreadLogger
{
    private static ILogger UnityLogger => Debug.unityLogger;

    public static async Task LogInformationAsync(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!UnityMainThreadDispatcher.Exists())
            return;

        var dispatcher = UnityMainThreadDispatcher.Instance();
        await dispatcher.ExecuteOrEnqueueIfNotMainThread(() =>
        {
            UnityLogger.Log(LogType.Log, $"[{memberName}] {message} ({System.IO.Path.GetFileName(filePath)}:{lineNumber})");
        }, dispatcher.CancellationToken);
    }

    public static void LogInformation(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!UnityMainThreadDispatcher.Exists())
            return;

        var dispatcher = UnityMainThreadDispatcher.Instance();
        _ = dispatcher.ExecuteOrEnqueueIfNotMainThread(() =>
        {
            UnityLogger.Log(LogType.Log, $"[{memberName}] {message} ({System.IO.Path.GetFileName(filePath)}:{lineNumber})");
        }, dispatcher.CancellationToken);
    }

    public static async Task LogExceptionAsync(
        Exception exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!UnityMainThreadDispatcher.Exists())
            return;

        var dispatcher = UnityMainThreadDispatcher.Instance();
        await dispatcher.ExecuteOrEnqueueIfNotMainThread(() =>
        {
            UnityLogger.Log(LogType.Error,
                $"[{memberName}] Exception in {System.IO.Path.GetFileName(filePath)}:{lineNumber}");

            UnityLogger.LogException(exception);
        }, dispatcher.CancellationToken);
    }

    public static void LogException(
        Exception exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!UnityMainThreadDispatcher.Exists())
            return;

        var dispatcher = UnityMainThreadDispatcher.Instance();
        _ = dispatcher.ExecuteOrEnqueueIfNotMainThread(() =>
        {
            UnityLogger.Log(LogType.Error,
                $"[{memberName}] Exception in {System.IO.Path.GetFileName(filePath)}:{lineNumber}");

            UnityLogger.LogException(exception);
        }, dispatcher.CancellationToken);
    }
}
