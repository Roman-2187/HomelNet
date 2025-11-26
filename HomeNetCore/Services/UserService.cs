
using HomeNetCore.Data.Repositories;
using HomeNetCore.Helpers;
using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Helpers.Exeptions;
using HomeNetCore.Models;
namespace HomeNetCore.Services
{
    public class UserService
    {
        private readonly ILogger _logger;

        private readonly UserRepository _repo;

        public UserService(UserRepository repo,ILogger logger) 
        {
            _logger = logger;
            _repo = repo ?? throw new ArgumentNullException(nameof(repo), "Repository не может быть null");
        }


        public Task<List<UserEntity>> GetAllUsersAsync()
        {
            return Task.Run(() =>
            {
                var users = _repo.GetAllAsync(); // Синхронный вызов
                if (users == null)
                    throw new InvalidOperationException("Репозиторий вернул null");
               
                return users;
            });
        }


        public async Task AddUserAsync(UserEntity user)
        {
            // Проверяем существование email
            if (await _repo.EmailExistsAsync(user.Email))
            {
                throw new DuplicateEmailException($"Email {user.Email} уже существует");
            }

            try
            {
                await _repo.InsertUserAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при добавлении пользователя: {ex.Message}");
                throw;
            }
        }


        public async Task<UserEntity?> FindUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email обязателен");

            return await _repo.GetByEmailAsync(email);
        }



        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _repo.EmailExistsAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при проверке email {email}: {ex.Message}");
                throw;
            }
        }


        public async Task DeleteUserAsync(int userId)
        {
            try
            {
                await _repo.DeleteByIdAsync(userId);
               
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Попытка удалить несуществующего пользователя",ex.Message );
                throw;
            }
        }


        public async Task<UserEntity?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _repo.GetByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError( "Ошибка при получении пользователя с ID {UserId}", userId.ToString(),ex.Message);
                throw;
            }
        }

    }

}
