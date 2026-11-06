using System;
using System.IO;
using System.Linq;
using System.Text;
using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;

namespace HomeNet.Infrastructure.Shared.Diagnostics
{
    /// <summary>
    /// Боевой одноствольный логгер SiberNet Engine для записи на диск.
    /// Генерирует интерактивный HTML-журнал со штампом места вызова. 🪐
    /// </summary>
    public class HtmlLogger : ICastomLogger
    {
        private readonly object _fileLock = new();
        private readonly string _debugFolderPath;
        private readonly string _htmlPath;

        private bool _isHtmlInitialized = false;

        /// <summary>
        /// Инициализирует HTML-логгер и гарантирует чистый запуск сессии.
        /// </summary>
        public HtmlLogger(string logName = "app_debug")
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _debugFolderPath = Path.Combine(desktopPath, "debug");
            _htmlPath = Path.Combine(_debugFolderPath, $"{logName}.html");

            // 🔥 ЧИСТЫЙ ЗАПУСК: Если старый файл лога существует, сносим его к чертям!
            try
            {
                if (!Directory.Exists(_debugFolderPath)) Directory.CreateDirectory(_debugFolderPath);
                if (File.Exists(_htmlPath)) File.Delete(_htmlPath);
            }
            catch
            {
                // Пуленепробиваемый щит, если файл заблокирован другой системой
            }
        }

        public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0)
        {
            // 1. Вытаскиваем имя класса из пути к файлу
            string className = string.IsNullOrEmpty(filePath)
                ? "UnknownClass"
                : Path.GetFileNameWithoutExtension(filePath).Split('.').Last() ?? "Unknown";

            // 2. Вытаскиваем имя папки (Наш архитектурный Namespace) прямо на месте вызова
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

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper();

            lock (_fileLock)
            {
                try
                {
                    // КАНАЛ ВЫВОДА: Пишем строго в интерактивный HTML-журнал с оригинальной красотой
                    EnsureHtmlHeaderInitialized(Path.GetFileNameWithoutExtension(_htmlPath));

                    string cssClass = level.ToString().ToLower();
                    string safeMessage = message.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
                    string htmlLine = $"        <span class='log-row {cssClass}'>[{timestamp}] [{levelStr}] [{detectedNamespace} -> {className}.{memberName}:{lineNumber}] | {safeMessage}</span>{Environment.NewLine}";

                    byte[] bytes = Encoding.UTF8.GetBytes(htmlLine);
                    using var stream = new FileStream(_htmlPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.WriteThrough);
                    stream.Write(bytes, 0, bytes.Length);
                }
                catch
                {
                    // Защита рантайма от случайных блокировок диска
                }
            }
        }

        private void EnsureHtmlHeaderInitialized(string logName)
        {
            if (_isHtmlInitialized || File.Exists(_htmlPath)) { _isHtmlInitialized = true; return; }

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='utf-8'>");
            sb.AppendLine($"   <title>SiberNet Log: {logName}</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { background-color: #1e1e1e; color: #d4d4d4; font-family: 'Consolas', 'Courier New', monospace; padding: 20px; line-height: 1.5; }");
            sb.AppendLine("        h1 { color: #569cd6; border-bottom: 2px solid #3e3e3e; padding-bottom: 10px; font-size: 20px; margin-bottom: 20px; }");
            sb.AppendLine("        .log-row { display: block; padding: 4px 8px; border-radius: 4px; margin: 3px 0; font-size: 15px; white-space: pre-wrap; font-weight: 500; }");
            sb.AppendLine("        .info { color: #4ec9b0; background-color: rgba(78, 201, 176, 0.04); border-left: 3px solid #4ec9b0; }");
            sb.AppendLine("        .debug { color: #9cdcfe; background-color: rgba(156, 220, 254, 0.02); border-left: 3px solid #9cdcfe; }");
            sb.AppendLine("        .warning { color: #dcdcaa; background-color: rgba(220, 220, 170, 0.03); border-left: 3px solid #dcdcaa; }");
            sb.AppendLine("        .error { color: #f44336; background-color: rgba(244, 67, 54, 0.08); font-weight: bold; border-left: 3px solid #f44336; }");
            sb.AppendLine("        .critical { color: #ffffff; background-color: #8b0000; font-weight: bold; border-left: 3px solid #ff0000; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"    <h1>Журнал Событий Сессии: {logName}.html</h1>");
            sb.AppendLine("    <div id='log-container'>");

            File.WriteAllText(_htmlPath, sb.ToString(), Encoding.UTF8);
            _isHtmlInitialized = true;
        }
    }
}
