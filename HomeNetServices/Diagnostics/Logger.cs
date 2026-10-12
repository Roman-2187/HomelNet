using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Diagnostics;
using System;
using System.IO;
using System.Linq;

namespace HomeNetServices.Diagnostics
{
    /// <summary>
    /// Боевой логгер системы. Больше никаких заглушек и NotImplementedException.
    /// </summary>
    public class Logger : ILogger
    {
        private readonly Action<string, LogLevel, string> _output;

        // Конструктор намертво привязывает кабель вывода логов
        public Logger(Action<string, LogLevel, string> output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output), "Вывод не может быть null");
            System.Diagnostics.Debug.WriteLine("[Logger] Инициализирован боевой вывод.");
        }

        /// <summary>
        /// Основной метод логирования. Сюда прилетают все вызовы LogInfo, LogError и т.д.
        /// </summary>
        public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0, params object[] args)
        {
            string formattedMessage = args != null && args.Length > 0 ? string.Format(message, args) : message;

            // Вытаскиваем имя класса
            string className = string.IsNullOrEmpty(filePath)
                ? "UnknownClass"
                : Path.GetFileNameWithoutExtension(filePath).Split('.').Last() ?? "Unknown";

            // Вытаскиваем имя папки (Архитектурный Namespace)
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

            // Безопасно пуляем данные в делегат вывода
            _output.Invoke(logEntry, level, detectedNamespace);
        }

        /// <summary>
        /// Реализация старого метода SetOutput для совместимости с интерфейсом ILogger.
        /// Просто игнорируем его, так как мы передаем вывод через конструктор.
        /// </summary>
        public void SetOutput(Action<string, LogLevel, string> output)
        {
            // Больше не кидает NotImplementedException! Просто молча выходит.
        }
    }
}
