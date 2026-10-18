using System.Runtime.CompilerServices;
using HomeNetCore.Enums;

namespace HomeNetCore.Interfaces.Diagnostics
{
    public interface ILogger
    {
      

        void Log(
            LogLevel level,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            params object[] args);
    }
}
