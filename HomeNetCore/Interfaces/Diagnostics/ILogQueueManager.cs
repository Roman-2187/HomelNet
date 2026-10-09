using HomeNetCore.Enums;

namespace HomeNetCore.Interfaces.OutputLogging
{


    
    
        public interface ILogQueueManager
        {
            void SetReady();
            void WriteLog(string message, LogLevel level, string namespaceName);

            // 🔥 ОБНОВЛЕННЫЙ РЕКОРД ДЛЯ АВТОБУСА СОБЫТИЙ:
            public record LogMessageReceived(string Text, LogLevel Level, string Namespace, bool IsAnimating);
        }
    }

