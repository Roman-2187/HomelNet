using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Models;

namespace HomeNetServices.Services.Identity
{
    public class UserService(IUserRepository repo, ILogger logger) : IUserService
    {
        private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IUserRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        public async Task<List<UserEntity>> GetAllAsync()
        {
            try
            {
                var users = await _repo.GetAllAsync()
                    ?? throw new InvalidOperationException("Репозиторий вернул null");

                _logger.LogInformation($"Получено {users.Count} пользователей.");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при получении пользователей из БД", ex.Message);
                throw;
            }
        }

        public async Task InsertUserAsync(UserEntity user)
        {
            try
            {
                await _repo.InsertUserAsync(user);
                _logger.LogDebug($"Пользователь {user.FirstName} успешно вставлен.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при добавлении пользователя: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteByIdAsync(int userId)
        {
            try
            {
                await _repo.DeleteByIdAsync(userId);
                _logger.LogInformation($"Пользователь с ID {userId} удалён.");
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Попытка удалить несуществующего пользователя", ex.Message);
                throw;
            }
        }

        public async Task<UserEntity?> GetByIdAsync(int userId)
        {
            try
            {
                return await _repo.GetByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при получении пользователя с ID {UserId}. {Message}", userId.ToString(), ex.Message);
                throw;
            }
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            try
            {
                return await _repo.GetByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при получении пользователя по Email {Email}. {Message}", email, ex.Message);
                throw;
            }
        }
    }
}
