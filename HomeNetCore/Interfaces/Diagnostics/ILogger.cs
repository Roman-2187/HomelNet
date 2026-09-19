using System.Runtime.CompilerServices;
using HomeNetCore.Enums;

namespace HomeNetCore.Interfaces.Diagnostics
{
    public interface ILogger
    {
        // 🔥 Добавили третий параметр (namespace) в Action
        void SetOutput(Action<string, LogLevel, string> output);

        void Log(
            LogLevel level,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args);
    }
}
