using HomeNetCore.Data.Interfaces;
using HomeNetCore.Data.Repositories;
using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Models;
namespace HomeNetCore.Services
{
    public class UserService(UserRepository repo, ILogger logger)
    {
        private readonly ILogger _logger = logger;

        private readonly UserRepository _repo = repo
            ?? throw new ArgumentNullException(nameof(repo), "Repository не может быть null");

        public async Task<List<UserEntity>> GetAllUsersAsync()
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
        

        public async Task AddUserAsync(UserEntity user)
        {            
            ArgumentNullException.ThrowIfNull(user.Email);
                       
            if (await _repo.EmailExistsAsync(user.Email))
            {
                _logger.LogDebug($"Email  {user.Email}  уже зарегистрирован ");
              
                throw new DuplicateEmailException(user.Email);
            }

            try
            {
                await _repo.InsertUserAsync(user);

                _logger.LogDebug($"Пользователь {user.FirstName}: успешно вставлен");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при добавлении пользователя: {ex.Message}");
                throw;
            }
        }


        public async Task<UserEntity?> FindUserByEmailAsync(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? throw new ArgumentException("Email обязателен") 
                : await _repo.GetByEmailAsync(email);
        }


        public async Task<bool> CheckEmailExistsAsync(string? email)
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


        public async Task DeleteUserAsync(int userId,string userName)
        {
            try
            {
                await _repo.DeleteByIdAsync(userId);

                _logger.LogInformation($"Пользователь{userName} с ID {userId} удалён.");

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


        public async Task<UserEntity?> GetUserByEmailAsync(string userEmail)
        {
            try
            {
                return await _repo.GetByEmailAsync(userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при получении пользователя с ID {UserEmail}", userEmail.ToString(), ex.Message);
                throw;
            }
        }

    }
}
