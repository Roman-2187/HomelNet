using System;
using System.IO;
using System.Text;
using HomeNetCore.Enums;

namespace HomeNetServices.Diagnostics
{
    /// <summary>
    /// Ультимативный статический хаб логирования SiberNet Engine.
    /// Фигачит цветной лог в консоль и мгновенно дампит сочный HTML на Рабочий стол. 🪐
    /// </summary>
    public static class DiagnosticHub
    {
        private static readonly object _fileLock = new();
        private static string _htmlPath = string.Empty;
        private static bool _isInitialized = false;

        /// <summary>
        /// Создает стильный HTML-шаблон в темной теме при самом первом вызове лога
        /// </summary>
        private static void EnsureInitialized(string logName)
        {
            if (_isInitialized) return;

            lock (_fileLock)
            {
                if (_isInitialized) return;

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string debugFolderPath = Path.Combine(desktopPath, "debug");
                
                // Гарантируем, что файл всегда будет с расширением .html
                _htmlPath = Path.Combine(debugFolderPath, $"{logName}.html");

                try
                {
                    if (!Directory.Exists(debugFolderPath))
                    {
                        Directory.CreateDirectory(debugFolderPath);
                    }

                    // Пересоздаем чистый HTML-лог с темным интерфейсом Visual Studio
                    var sb = new StringBuilder();
                    sb.AppendLine("<!DOCTYPE html>");
                    sb.AppendLine("<html>");
                    sb.AppendLine("<head>");
                    sb.AppendLine("    <meta charset='utf-8'>");
                    sb.AppendLine($"   <title>SiberNet Log: {logName}</title>");
                    sb.AppendLine("    <style>");
                    sb.AppendLine("        body { background-color: #1e1e1e; color: #d4d4d4; font-family: 'Consolas', 'Courier New', monospace; padding: 20px; line-height: 1.5; }");
                    sb.AppendLine("        h1 { color: #569cd6; border-bottom: 2px solid #3e3e3e; padding-bottom: 10px; font-size: 20px; margin-bottom: 20px; }");
                    sb.AppendLine("        .log-row { display: block; padding: 4px 8px; border-radius: 4px; margin: 3px 0; font-size: 16px; white-space: pre-wrap; font-weight: 500; }");
                    // Стилизация строк лога под уровень важности
                    sb.AppendLine("        .info { color: #4ec9b0; background-color: rgba(78, 201, 176, 0.04); border-left: 3px solid #4ec9b0; }");
                    sb.AppendLine("        .debug { color: #9cdcfe; background-color: rgba(156, 220, 254, 0.02); border-left: 3px solid #9cdcfe; }");
                    sb.AppendLine("        .warning { color: #dcdcaa; background-color: rgba(220, 220, 170, 0.03); border-left: 3px solid #dcdcaa; }");
                    sb.AppendLine("        .error { color: #f44336; background-color: rgba(244, 67, 54, 0.08); font-weight: bold; border-left: 3px solid #f44336; }");
                    sb.AppendLine("    </style>");
                    sb.AppendLine("</head>");
                    sb.AppendLine("<body>");
                    sb.AppendLine($"    <h1>Журнал Событий Сессии: {logName}.html</h1>");
                    sb.AppendLine("    <div id='log-container'>");

                    File.WriteAllText(_htmlPath, sb.ToString(), Encoding.UTF8);
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DiagnosticHub] Не удалось поднять HTML-лог: {ex.Message}");
                }
                finally
                {
                    _isInitialized = true;
                }
            }
        }

        public static void LogInfo(string message, string logName = "app_debug") => Log(message, LogLevel.Info, logName);
        public static void LogDebug(string message, string logName = "app_debug") => Log(message, LogLevel.Debug, logName);
        public static void LogWarning(string message, string logName = "app_debug") => Log(message, LogLevel.Warning, logName);
        public static void LogError(string message, string logName = "app_debug") => Log(message, LogLevel.Error, logName);

        /// <summary>
        /// Мясорубка логирования: параллельно разукрашивает консоль и делает WriteThrough-запись в HTML
        /// </summary>
        public static void Log(string message, LogLevel level, string logName = "app_debug")
        {
            if (string.IsNullOrEmpty(message)) return;

            EnsureInitialized(logName);

            // 1. ОДНОВРЕМЕННО СТРЕЛЯЕМ В КОНСОЛЬ (С сохранением оригинального цвета)
            lock (_fileLock)
            {
                Console.ForegroundColor = level switch
                {
                    LogLevel.Info => ConsoleColor.Green,
                    LogLevel.Debug => ConsoleColor.Cyan,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };
                Console.WriteLine(message);
                Console.ResetColor();
            }

            // 2. УПАКОВЫВАЕМ СТРОКУ В CSS-РАЗМЕТКУ
            string cssClass = level.ToString().ToLower();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string safeMessage = message.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            
            string htmlLine = $"        <span class='log-row {cssClass}'>[{timestamp}] [{level.ToString().ToUpper()}] {safeMessage}</span>{Environment.NewLine}";

            // 3. МГНОВЕННЫЙ СБРОС НА ЖЕСТКИЙ ДИСК КЛИЕНТА/СЕРВЕРА
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(htmlLine);
                lock (_fileLock)
                {
                    using var stream = new FileStream(
                        _htmlPath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite,
                        bufferSize: 4096,
                        FileOptions.WriteThrough);

                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch
            {
                // Защита рантайма от блокировок диска
            }
        }
    }
}
