using HomeNet.Infrastructure.Shared.Enums;

namespace HomeNetCore.Interfaces.Diagnostics
{ 
        public interface ILogQueueManager
        {
            void SetReady();

        public void WriteLog(string message, LogLevel level, string namespaceName);
          

            // 🔥 ОБНОВЛЕННЫЙ РЕКОРД ДЛЯ АВТОБУСА СОБЫТИЙ:
            public record LogMessageReceived(string Text, LogLevel Level, string Namespace, bool IsAnimating);
        }
    }

