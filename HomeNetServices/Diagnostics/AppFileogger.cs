using System;
using System.IO;
using System.Text;
using HomeNetCore.Enums;

namespace HomeNetServices.Diagnostics
{
    public class AppFileLogger
    {
        private readonly string _filePath;
        private readonly object _fileLock = new();

        /// <summary>
        /// Конструктор теперь принимает просто ИМЯ файла. 
        /// Логгер сам положит его в папку 'debug' на Рабочем столе.
        /// </summary>
        public AppFileLogger(string fileName = "File_debug.txt")
        {
            // Инкапсулируем логику формирования путей внутри класса
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string debugFolderPath = Path.Combine(desktopPath, "debug");

            _filePath = Path.Combine(debugFolderPath, fileName);

            lock (_fileLock)
            {
                try
                {
                    if (!Directory.Exists(debugFolderPath))
                    {
                        Directory.CreateDirectory(debugFolderPath);
                    }

                    // Очищаем или создаем пустой файл при старте
                    using (new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AppFileLogger] Ошибка инициализации файла лога: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Метод для серверных логов: Добавляет готовую строку без изменений
        /// </summary>
        public void WriteRawImmediately(string rawLogMessage)
        {
            if (string.IsNullOrEmpty(rawLogMessage)) return;

            try
            {
                string logLine = $"{rawLogMessage}{Environment.NewLine}{Environment.NewLine}";
                WriteToFile(logLine);
            }
            catch
            {
                // Защита от сбоев диска
            }
        }

        /// <summary>
        /// Стандартный метод для локальных клиентских логов
        /// </summary>
        public void WriteImmediately(string message, LogLevel level)
        {
            try
            {
                // При желании сюда можно добавить префикс уровня лога: $"[{level}] {message}"
                string logLine = $"{message}{Environment.NewLine}{Environment.NewLine}";
                WriteToFile(logLine);
            }
            catch
            {
                // Защита от сбоев диска
            }
        }

        /// <summary>
        /// Выделенный приватный метод записи, чтобы не дублировать код FileStream
        /// </summary>
        private void WriteToFile(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);

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
    }
}
