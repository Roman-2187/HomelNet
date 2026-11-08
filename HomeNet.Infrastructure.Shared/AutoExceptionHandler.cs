using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using System.Runtime.CompilerServices;

namespace HomeNet.Infrastructure.Shared
{
    public static class AutoExceptionHandler
    {
        public static async Task<T> ExecuteThrowAsync<T>(
            ICastomLogger logger,
            Func<Task<T>> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) // <--- Добавили)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                // Никакого StackTrace. Передаем ровно то, что дал компилятор.
                // Это будет вызывающий класс (MessageRepository) и его метод.
                logger?.Log(LogLevel.Error, ex.Message, memberName, filePath, lineNumber);
                throw;
            }
        }

        public static async Task<T> ExecuteSafeAsync<T>(
            ICastomLogger logger,
            Func<Task<T>> action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
              [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger?.Log(LogLevel.Error, ex.Message, memberName, filePath, lineNumber);

                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return (T)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(T).GetGenericArguments()[0]))!;
                }
                return default!;
            }
        }
    }
}