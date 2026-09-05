using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeNetCore.Services
{
   
        public class DeleteService
        {
            private readonly UserService _userService;
              ILogger _logger;
            public DeleteService(ILogger iloger, UserService userService)
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
                    var user = await _userService.GetUserByIdAsync(id);

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
                    await _userService.DeleteUserAsync(id);

                // 1. Дождались, пока СУБД выплюнет список в оперативку
                var allUsers = await _userService.GetAllUsersAsync();

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

