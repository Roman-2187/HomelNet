using HomeNetCore.Interfaces;
using HomeNetCore.Models.Entity;

namespace HomeNetCore.Extensions
{
    public static class UserServiceExtensions
    {
        /// <summary>
        /// Безопасная отправка пользователя в сервис с валидацией входных аргументов на null.
        /// Проверка на существование вырезана — делегирована репозиторию! 🧼
        /// </summary>
        public static async Task AddUserSecureAsync(this IUserService userService, UserEntity user)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(user.Email);

            await userService.InsertUserAsync(user);
        }

        /// <summary>
        /// Безопасный поиск по Email с валидацией входной строки
        /// </summary>
        public static async Task<UserEntity?> FindUserByEmailAsync(this IUserService userService, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email обязателен для поиска", nameof(email));

            return await userService.GetByEmailAsync(email);
        }
    }
}
