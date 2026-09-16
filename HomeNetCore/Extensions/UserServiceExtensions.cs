using HomeNetCore.Exeptions;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;

namespace HomeNetCore.Extensions
{
    public static class UserServiceExtensions
    {
        // 1. Удобная обертка для добавления с проверкой дубликата Email
        public static async Task AddUserSecureAsync(this IUserService userService, UserEntity user)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(user.Email);

            if (await userService.CheckEmailExistsAsync(user.Email))
            {
                throw new DuplicateEmailException(user.Email);
            }

            await userService.InsertUserAsync(user);
        }

        // 2. Быстрая проверка существования email
        public static async Task<bool> CheckEmailExistsAsync(this IUserService userService, string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var user = await userService.GetByEmailAsync(email);
            return user != null;
        }

        // 3. Безопасный поиск по Email с валидацией входной строки
        public static async Task<UserEntity?> FindUserByEmailAsync(this IUserService userService, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email обязателен для поиска", nameof(email));

            return await userService.GetByEmailAsync(email);
        }
    }
}
