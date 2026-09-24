using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models;

namespace HomeNetServices.Services.Identity
{

    public class DeleteService : IDeleteService
    {
        private readonly IUserService _userService;
        ILogger _logger;
        public DeleteService(ILogger iloger, IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = iloger ?? throw new ArgumentNullException();
        }

        // Безопасный поиск пользователя для формы удаления
        public async Task<(bool IsSuccess, string Message, UserEntity? User)> SearchUserAsync(string targetUserId)
        {
            if (string.IsNullOrWhiteSpace(targetUserId))
                return (false, "Ошибка: ID не может быть пустым!", null);

            if (!int.TryParse(targetUserId, out int id))
                return (false, "Ошибка: ID должен состоять только из цифр!", null);

            try
            {
                var user = await _userService.GetByIdAsync(id);

                if (user != null)
                    return (true, $"Найден: {user.FirstName} {user.LastName} ({user.Email})", user);

                return (false, $"Пользователь с ID {id} не существует.", null);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка базы данных: {ex.Message}", null);
            }
        }

        // Безопасное удаление из БД
        public async Task<(bool IsSuccess, string Message)> DeleteUserAsync(int id)
        {
            if (id <= 0)
                return (false, "Ошибка: Некорректный ID пользователя!");

            try
            {
                await _userService.DeleteByIdAsync(id);

                // 1. Дождались, пока СУБД выплюнет список в оперативку
                var allUsers = await _userService.GetAllAsync();

                // 2. И теперь у нормального списка спокойно берем Count! 🚀
                _logger.LogDebug($"в системе {allUsers.Count} пользователей");

                return (true, $"Пользователь с ID {id} успешно удален из системы.");


            }
            catch (Exception ex)
            {
                return (false, $"Ошибка удаления из БД: {ex.Message}");
            }

        }
    }
}

