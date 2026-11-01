using HomeNet.Infrastructure.Shared.Diagnostics;
using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using System.Runtime.CompilerServices;

namespace HomeNet.Infrastructure.Shared.Interceptors
{
    public class AutoLoggerInterceptor
    {
        private readonly ILogger _baseLogger;

        public AutoLoggerInterceptor(ILogger baseLogger)
        {
            _baseLogger = baseLogger ?? throw new ArgumentNullException(nameof(baseLogger));
        }

        // Логируем кастомную строку через сборщик автолога
        private void LogAuto(LogLevel level, string rawMessage, string memberName, string filePath, int lineNumber)
        {
            // Собираем строку принудительно со штампом [АВТОЛОГ]
            string autoMessage = LogFormatter.Format(rawMessage, memberName, filePath, lineNumber, isAutoLog: true);

            // Отправляем в базовый логгер (он просто выведет её в консоль/файл)
            _baseLogger.Log(level, autoMessage);
        }

        public async Task<T> ExecuteAsync<T>(object caller, Func<Task<T>> action, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            LogAuto(LogLevel.Info, "[СТАРТ] Операция запущенна автоматикой.", memberName, filePath, lineNumber);
            try
            {
                T result = await action();
                LogAuto(LogLevel.Info, "[УСПЕХ] Завершено.", memberName, filePath, lineNumber);
                return result;
            }
            catch (Exception ex)
            {
                LogAuto(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] Сбой! Ошибка: {ex.Message}", memberName, filePath, lineNumber);
                throw;
            }
        }

        public async Task<(TResult? Data, string? Error)> GuardAsync<TResult>(object caller, Func<Task<TResult>> action, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) where TResult : class
        {
            LogAuto(LogLevel.Info, $"[СТАРТ ОРМ/СЕТЬ] Метод вызван.", memberName, filePath, lineNumber);
            try
            {
                var result = await action();
                LogAuto(LogLevel.Info, $"[УСПЕХ ОРМ/СЕТЬ] Отработало штатно.", memberName, filePath, lineNumber);
                return (result, null);
            }
            catch (Exception ex)
            {
                string errMsg = $"Сбой операции автоматики: {ex.Message}";
                LogAuto(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] {errMsg}", memberName, filePath, lineNumber);
                return (null, errMsg);
            }
        }
    }
}
