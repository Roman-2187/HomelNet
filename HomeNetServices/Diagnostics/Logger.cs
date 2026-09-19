using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Diagnostics;

namespace HomeNetServices.Diagnostics
{
   

   
        public class Logger : ILogger
        {
            private Action<string, LogLevel, string>? _output;

            public Logger()
            {
                System.Diagnostics.Debug.WriteLine("[Logger] Инициализирован вывод по умолчанию.");
            }

            public void SetOutput(Action<string, LogLevel, string> output)
            {
                _output = output ?? throw new ArgumentNullException(nameof(output), "Вывод не может быть null");
            }

            public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0, params object[] args)
            {
                string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;

                // Вытаскиваем имя класса
                string className = string.IsNullOrEmpty(filePath)
                    ? "UnknownClass"
                    : Path.GetFileNameWithoutExtension(filePath).Split('.').Last() ?? "Unknown";

                // 🔥 ВЫТАСКИВАЕМ ИМЯ ПАПКИ (Архитектурный Namespace)
                string detectedNamespace = "Core";
                if (!string.IsNullOrEmpty(filePath))
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        detectedNamespace = Path.GetFileName(directory) ?? "Core";
                    }
                }

                className = className.Replace('_', ' ').Trim();
                memberName = memberName.Replace('_', ' ').Trim();

                var timestamp = DateTime.UtcNow.ToString("MM/dd HH:mm:ss.fff");
                var levelStr = level.ToString().ToUpper();

                var logEntry = $"[{timestamp}] [{levelStr}] [{detectedNamespace} -> {className}.{memberName}:{lineNumber}] | {formattedMessage}";

                // 🔥 Просто пуляем данные в кабель, улетит и текст, и уровень, и папка!
                _output?.Invoke(logEntry, level, detectedNamespace);
            }
        }
    }


