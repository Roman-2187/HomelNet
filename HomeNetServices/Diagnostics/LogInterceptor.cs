using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Diagnostics;

namespace HomeNetServices.Diagnostics
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

        /// <summary>
        /// Выполняет и логирует АСИНХРОННЫЙ метод, который ВОЗВРАЩАЕТ результат (Task от T)
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            object caller,
            Func<Task<T>> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string className = caller?.GetType().Name ?? "UnknownClass";

            // Авто-лог перед запуском операции
            _logger.Log(LogLevel.Info, $"[СТАРТ] Операция запущена.", memberName, filePath, lineNumber);

            try
            {
                T result = await action();

                // Авто-лог успешного завершения
                _logger.Log(LogLevel.Info, $"[УСПЕХ] Операция успешно завершена.", memberName, filePath, lineNumber);
                return result;
            }
            catch (Exception ex)
            {
                // Мгновенный перехват и детальный лог ошибки с пробросом исключения дальше
                _logger.Log(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] Сбой в {className}.{memberName}! Ошибка: {ex.Message} | StackTrace: {ex.StackTrace}", memberName, filePath, lineNumber);
                throw;
            }
        }

        /// <summary>
        /// Выполняет и логирует АСИНХРОННЫЙ метод, который НЕ возвращает результат (Task)
        /// </summary>
        public async Task ExecuteAsync(
            object caller,
            Func<Task> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string className = caller?.GetType().Name ?? "UnknownClass";
            _logger.Log(LogLevel.Info, $"[СТАРТ] Вошли в метод.", memberName, filePath, lineNumber);

            try
            {
                await action();
                _logger.Log(LogLevel.Info, $"[КОНЕЦ] Метод отработал штатно.", memberName, filePath, lineNumber);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] Ошибка выполнения: {ex.Message}", memberName, filePath, lineNumber);
                throw;
            }
        }

        /// <summary>
        /// Выполняет и логирует СИНХРОННЫЙ метод с возвращаемым значением
        /// </summary>
        public T Execute<T>(
            object caller,
            Func<T> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string className = caller?.GetType().Name ?? "UnknownClass";
            _logger.Log(LogLevel.Info, $"[СТАРТ] Синхронный вызов.", memberName, filePath, lineNumber);

            try
            {
                T result = action();
                _logger.Log(LogLevel.Info, $"[УСПЕХ] Синхронный вызов завершен.", memberName, filePath, lineNumber);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"[КРИТ ИСКЛЮЧЕНИЕ] Ошибка: {ex.Message}", memberName, filePath, lineNumber);
                throw;
            }
        }
    }
}
