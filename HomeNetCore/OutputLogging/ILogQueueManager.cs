using HomeNetCore.Enums;

namespace HomeNetCore.Interfaces.OutputLogging
{
    
    
        public interface ILogQueueManager
        {
            // 🔥 Добавили string (namespace) третьим параметром в делегат!
            event Func<string, LogLevel, string, bool, Task>? OnLogReceived;

            // 🔥 Метод теперь тоже обязан принимать имя папки/неймспейса
            void WriteLog(string message, LogLevel level, string namespaceName);

            void SetReady();
            void Dispose();
        }
    }
