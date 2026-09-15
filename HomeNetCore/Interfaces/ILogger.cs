using System.Runtime.CompilerServices;
using HomeNetCore.Enums;

namespace HomeNetCore.Interfaces
{
    public interface ILogger
    {
        void SetOutput(Action<string, LogLevel> output);

        void Log(
            LogLevel level,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args);
    }
}
