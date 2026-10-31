using HomeNet.Infrastructure.Shared.Enums;
using System.Runtime.CompilerServices;

namespace HomeNet.Infrastructure.Shared.Interfaces.Diagnostics
{
    public interface ILogger
    {
        void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0, params object[] args);

        // Встроенные методы интерфейса (IntelliSense видит их везде без using)
        void LogDebug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params object[] args)
            => Log(LogLevel.Debug, message, memberName, filePath, lineNumber, args);

        void LogInfo(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params object[] args)
            => Log(LogLevel.Info, message, memberName, filePath, lineNumber, args);

        void LogWarning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params object[] args)
            => Log(LogLevel.Warning, message, memberName, filePath, lineNumber, args);

        void LogError(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params object[] args)
            => Log(LogLevel.Error, message, memberName, filePath, lineNumber, args);

        void LogCritical(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params object[] args)
            => Log(LogLevel.Critical, message, memberName, filePath, lineNumber, args);
    }
}
