using System;
using System.IO;
using System.Text;
using HomeNetCore.Enums;

namespace HomeNetServices.Diagnostics
{
    public class AppFileogger
    {
        private readonly string _filePath;
        private readonly object _fileLock = new();

        public AppFileogger(string filePath = "crash_debug.txt")
        {
            _filePath = filePath;

            // ГАРАНТИРОВАННОЕ ЗАТИРАНИЕ: Обнуляем файл при каждом новом старте приложения
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
                        // Просто открываем в режиме Create и закрываем, снося старое содержимое
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FileCrashLogger] Ошибка очистки файла: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Мгновенная, сквозная запись на физический диск в обход системного кэша Windows
        /// </summary>
        public void WriteImmediately(string message, LogLevel level)
        {
            try
            {
                // 🧼 ЧИСТЫЙ ТЕКСТ: Убрали ANSI Escape-коды. 
                // Добавляем ДВА переноса строки в конце (Environment.NewLine + Environment.NewLine),
                // чтобы между сообщениями была аккуратная пустая строка-разделитель.
                string logLine = $"{message}{Environment.NewLine}{Environment.NewLine}";
                byte[] bytes = Encoding.UTF8.GetBytes(logLine);

                lock (_fileLock)
                {
                    // FileOptions.WriteThrough приказывает ОС пушить данные на жесткий диск МГНОВЕННО
                    // FileShare.ReadWrite позволяет открывать txt в Блокноте параллельно с работой мессенджера
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
                // Сбой диска не должен обрушить основное приложение
            }
        }
    }
}
