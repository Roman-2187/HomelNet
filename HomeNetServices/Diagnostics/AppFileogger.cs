using System;
using System.IO;
using System.Text;
using HomeNetCore.Enums;

namespace HomeNetServices.Diagnostics
{
    public class AppFileLogger // Исправили опечатку в названии (был AppFileogger)
    {
        private readonly string _filePath;
        private readonly object _fileLock = new();

        public AppFileLogger(string filePath = "crash_debug.txt")
        {
            _filePath = filePath;

            lock (_fileLock)
            {
                try
                {
                    var directory = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FileCrashLogger] Ошибка очистки файла: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 🔥 МЕТОД ДЛЯ СЕРВЕРНЫХ ЛОГОВ: Добавляет готовую строку БЕЗ ИЗМЕНЕНИЙ и мясорубок!
        /// </summary>
        public void WriteRawImmediately(string rawLogMessage)
        {
            if (string.IsNullOrEmpty(rawLogMessage)) return;

            try
            {
                // Просто берем прилетевший готовый лог бэкенда и добавляем отступы
                string logLine = $"{rawLogMessage}{Environment.NewLine}{Environment.NewLine}";
                byte[] bytes = Encoding.UTF8.GetBytes(logLine);

                lock (_fileLock)
                {
                    using var stream = new FileStream(
                        _filePath,
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
                // Защита от сбоев диска
            }
        }

        /// <summary>
        /// Стандартный метод для локальных клиентских логов (с мясорубкой)
        /// </summary>
        public void WriteImmediately(string message, LogLevel level)
        {
            try
            {
                string logLine = $"{message}{Environment.NewLine}{Environment.NewLine}";
                byte[] bytes = Encoding.UTF8.GetBytes(logLine);

                lock (_fileLock)
                {
                    using var stream = new FileStream(
                        _filePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite,
                        bufferSize: 4096,
                        FileOptions.WriteThrough);

                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch { }
        }
    }
}
