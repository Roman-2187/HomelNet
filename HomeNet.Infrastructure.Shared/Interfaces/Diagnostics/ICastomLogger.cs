using HomeNet.Infrastructure.Shared.Enums;
using System.Runtime.CompilerServices;

namespace HomeNet.Infrastructure.Shared.Interfaces.Diagnostics
{
    public interface ICastomLogger
    {
        // Базовый ствол логгера, который будут реализовывать наши классы
        void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0);

        // Твои любимые встроенные методы интерфейса — на месте!
        void LogDebug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => Log(LogLevel.Debug, message, memberName, filePath, lineNumber);

        void LogInfo(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => Log(LogLevel.Info, message, memberName, filePath, lineNumber);

        void LogWarning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => Log(LogLevel.Warning, message, memberName, filePath, lineNumber);

        void LogError(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => Log(LogLevel.Error, message, memberName, filePath, lineNumber);

        void LogCritical(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => Log(LogLevel.Critical, message, memberName, filePath, lineNumber);
    }
}
