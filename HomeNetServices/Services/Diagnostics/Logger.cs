using System;
using System.IO;
using System.Linq;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces;

namespace HomeNetServices.Diagnostics.Logging
{
    public class Logger : ILogger
    {
        private Action<string, LogLevel>? _output;

        public Logger()
        {
            // По умолчанию пишем в стандартное окно отладки Visual Studio
            System.Diagnostics.Debug.WriteLine("[Logger] Инициализирован вывод по умолчанию.");
        }

        public void SetOutput(Action<string, LogLevel> output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output), "Вывод не может быть null");
        }

        // Реализуем единственный метод интерфейса 🧼✨
        public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0, params object[] args)
        {
            if (_output == null) return;

            string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;

            // Вытаскиваем имя класса на лету без рефлексии
            string className = string.IsNullOrEmpty(filePath)
                ? "UnknownClass"
                : Path.GetFileNameWithoutExtension(filePath).Split('.').Last() ?? "Unknown";

            className = className.Replace('_', ' ').Trim().Replace(".", " ").Replace("`", "");
            memberName = memberName.Replace('_', ' ').Trim().Replace(".", " ").Replace("`", "");

            var timestamp = DateTime.UtcNow.ToString("MM/dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper();

            var logEntry = $"[{timestamp}] [{levelStr}] [{className}.{memberName}:{lineNumber}] | {formattedMessage}";

            // Пуляем готовый текст и уровень лога в LogQueueManager
            _output(logEntry, level);
        }
    }
}
