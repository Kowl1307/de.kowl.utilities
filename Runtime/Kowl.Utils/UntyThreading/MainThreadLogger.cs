using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.UntyThreading
{
    public class UnityMainThreadLogger
    {
        private static ILogger UnityLogger
        {
            get
            {
                return Debug.unityLogger;
            }
        }

        public static async Task LogInformationAsync(string message, [CallerMemberName] object? context = null)
        {
            await UnityMainThreadDispatcher.Instance().ExecuteOrEnqueueIfNotMainThread(() =>
            {
                UnityLogger.Log(LogType.Log, message, context);
            });
        }

        public static void LogInformation(string message, [CallerMemberName] object? context = null)
        {
            _ = UnityMainThreadDispatcher.Instance().ExecuteOrEnqueueIfNotMainThread(() =>
            {
                UnityLogger.Log(LogType.Log, message, context);
            });
        }

        public static async Task LogWarningAsync(string message, [CallerMemberName] object? context = null)
        {
            await UnityMainThreadDispatcher.Instance().ExecuteOrEnqueueIfNotMainThread(() =>
            {
                UnityLogger.Log(LogType.Warning, message, context);
            });
        }

        public static void LogWarning(string message, [CallerMemberName] object? context = null)
        {
            _ = UnityMainThreadDispatcher.Instance().ExecuteOrEnqueueIfNotMainThread(() =>
            {
                UnityLogger.Log(LogType.Warning, message, context);
            });
        }

        public static async Task LogErrorAsync(Exception e)
        {
            await UnityMainThreadDispatcher.Instance().ExecuteOrEnqueueIfNotMainThread(() =>
            {
                UnityLogger.LogException(e);
            });
        }

        public static void LogError(Exception e)
        {
            _ = UnityMainThreadDispatcher.Instance().ExecuteOrEnqueueIfNotMainThread(() =>
            {
                UnityLogger.LogException(e);
            });
        }
    }
}
