using HomeNetCore.Enums;

namespace HomeNetServices.Services.Diagnostics
{
    public interface ILogQueueManager
    {
        event Func<string, LogLevel, bool, Task>? OnLogReceived;

        void Dispose();
        void SetReady();
        void WriteLog(string message, LogLevel level);
    }
}