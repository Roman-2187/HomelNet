using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using System.Runtime.CompilerServices;

namespace HomeNet.Infrastructure.Shared.Interceptors
{
    /// <summary>
    /// Автоматический перехватчик и мясорубка для методов репозиториев и сервисов.
    /// Полностью убирает необходимость писать ручные try-catch блоки и базовые логи.
    /// </summary>
    public class LogInterceptor
    {
        private readonly ILogger _logger;

        public LogInterceptor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T> ExecuteAsync<T>(
            object caller,
            Func<Task<T>> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string className = caller?.GetType().Name ?? "UnknownClass";
            _logger.Log(LogLevel.Info, "[СТАРТ] Операция запущена.", memberName, filePath, lineNumber);

            try
            {
                T result = await action();
                _logger.Log(LogLevel.Info, "[УСПЕХ] Операция успешно завершена.", memberName, filePath, lineNumber);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] Сбой в {className}.{memberName}! Ошибка: {ex.Message} | StackTrace: {ex.StackTrace}", memberName, filePath, lineNumber);
                throw;
            }
        }

        public async Task ExecuteAsync(
            object caller,
            Func<Task> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string className = caller?.GetType().Name ?? "UnknownClass";
            _logger.Log(LogLevel.Info, "[СТАРТ] Вошли в метод.", memberName, filePath, lineNumber);

            try
            {
                await action();
                _logger.Log(LogLevel.Info, "[КОНЕЦ] Метод отработал штатно.", memberName, filePath, lineNumber);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] Ошибка выполнения: {ex.Message}", memberName, filePath, lineNumber);
                throw;
            }
        }

        public T Execute<T>(
            object caller,
            Func<T> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string className = caller?.GetType().Name ?? "UnknownClass";
            _logger.Log(LogLevel.Info, "[СТАРТ] Синхронный вызов.", memberName, filePath, lineNumber);

            try
            {
                T result = action();
                _logger.Log(LogLevel.Info, "[УСПЕХ] Синхронный вызов завершен.", memberName, filePath, lineNumber);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] Ошибка: {ex.Message}", memberName, filePath, lineNumber);
                throw;
            }
        }

        /// <summary>
        /// Сеньорский реактивный страж. Заменяет try-catch рутину в репозиториях.
        /// Автоматически логирует процесс и пакует сбой в красивый текст для шины без падения потока.
        /// </summary>
        public async Task<(TResult? Data, string? Error)> GuardAsync<TResult>(
            object caller,
            Func<Task<TResult>> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) where TResult : class
        {
            string className = caller?.GetType().Name ?? "UnknownClass";
            _logger.Log(LogLevel.Info, $"[СТАРТ ОРМ/СЕТЬ] Операция {className}.{memberName} запущена.", memberName, filePath, lineNumber);

            try
            {
                var result = await action();
                _logger.Log(LogLevel.Info, $"[УСПЕХ ОРМ/СЕТЬ] Операция {className}.{memberName} завершена.", memberName, filePath, lineNumber);
                return (result, null);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Сбой операции [{className}.{memberName}]: {ex.Message}";
                _logger.Log(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] {errorMessage} | Стек: {ex.StackTrace}", memberName, filePath, lineNumber);
                return (null, errorMessage);
            }
        }
    }
}
