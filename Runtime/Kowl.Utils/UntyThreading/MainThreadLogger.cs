using Packages.de.kowl.utilities.Runtime.Kowl.Utils.UntyThreading;
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
        await UnityMainThreadDispatcher.Instance().ExecuteOrEnqueueIfNotMainThread(() =>
        {
            UnityLogger.Log(LogType.Log, $"[{memberName}] {message} ({System.IO.Path.GetFileName(filePath)}:{lineNumber})");
        });
    }

    public static void LogInformation(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        _ = UnityMainThreadDispatcher.Instance().ExecuteOrEnqueueIfNotMainThread(() =>
        {
            UnityLogger.Log(LogType.Log, $"[{memberName}] {message} ({System.IO.Path.GetFileName(filePath)}:{lineNumber})");
        });
    }
}