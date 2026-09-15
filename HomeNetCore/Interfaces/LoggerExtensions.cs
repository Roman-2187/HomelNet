using System.Runtime.CompilerServices;
using HomeNetCore.Enums;

namespace HomeNetCore.Interfaces
{
    public static class LoggerExtensions
    {
        public static void LogDebug(this ILogger logger, string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, params object[] args)
            => logger.Log(LogLevel.Debug, message, memberName, filePath, lineNumber, args);

        public static void LogInformation(this ILogger logger, string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, params object[] args)
            => logger.Log(LogLevel.Information, message, memberName, filePath, lineNumber, args);

        public static void LogWarning(this ILogger logger, string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, params object[] args)
            => logger.Log(LogLevel.Warning, message, memberName, filePath, lineNumber, args);

        public static void LogError(this ILogger logger, string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = 0, params object[] args)
            => logger.Log(LogLevel.Error, message, memberName, filePath, lineNumber, args);

        public static void LogCritical(this ILogger logger, string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, params object[] args)
            => logger.Log(LogLevel.Critical, message, memberName, filePath, lineNumber, args);
    }
}

