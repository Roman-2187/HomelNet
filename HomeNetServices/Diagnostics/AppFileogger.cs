using System.Text;
using HomeNetCore.Enums;

namespace HomeNetServices.Diagnostics
{
    public class AppFileogger
    {
        private readonly string _filePath;
        private readonly object _fileLock = new();

        // ANSI Escape-коды для раскраски текста (поддерживаются современными терминалами и плагинами VS Code)
        private const string Reset = "\u001b[0m";
        private const string Red = "\u001b[31m";       // Error
        private const string Orange = "\u001b[33m";    // Warning
        private const string Magenta = "\u001b[35m";   // Critical
        private const string Gray = "\u001b[90m";      // Info

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
                // Подбираем цвет под строгость лога
                string colorCode = level switch
                {
                    LogLevel.Error => Red,
                    LogLevel.Warning => Orange,
                    LogLevel.Critical => Magenta,
                    _ => Gray
                };

                // Оборачиваем строку в ANSI-код и добавляем системный перенос строки
                string logLine = $"{colorCode}{message}{Reset}{Environment.NewLine}";
                byte[] bytes = Encoding.UTF8.GetBytes(logLine);

                lock (_fileLock)
                {
                    // FileOptions.WriteThrough приказывает ОС пушить данные на жесткий диск МГНОВЕННО
                    // FileShare.ReadWrite позволяет открывать txt в Блокноте/VS Code параллельно с работой мессенджера
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
