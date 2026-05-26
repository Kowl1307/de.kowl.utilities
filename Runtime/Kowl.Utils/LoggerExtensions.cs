using System.Runtime.CompilerServices;
using UnityEngine;

#nullable enable

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils
{
    public static class LoggerExtensions
    {
        public static void LogInformation(this ILogger logger, string message, [CallerMemberName] object? context = null)
        {
            logger.Log(LogType.Log, message, context);
        }
    }
}
