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


        public Task<List<UserEntity>> GetAllUsersAsync()
        {
            try
            {
                 return Task.Run(async () =>
                {
                    var users = await _repo.GetAllAsync()
                    ?? throw new InvalidOperationException("Репозиторий вернул null");
                    _logger.LogInformation($"Получено {users.Count} пользователей.");
                    return users;
                });

            }
            catch (Exception ex)
            {
                 _logger.LogError("Ошибка при получении пользователей из БД", ex.Message);
                throw;                
            }
        }


       public async Task<(bool success, string userName)> ValidateCredentialsAsync(string email, string password)
        {
            try
            {            
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Попытка авторизации с пустыми учетными данными");
                    return (false, string.Empty);
                }

                var user = await _repo.GetByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogInformation($"Попытка входа с несуществующим email: {email}");
                    return (false, string.Empty);
                }
                               
                bool isValid = user.Password == password;

                if (isValid)
                {                   
                    _logger.LogInformation($"Успешная авторизация пользователя: {user.FullName}");

                    return (true, user.FullName??"Null impossible");
                }
                else
                {
                    _logger.LogInformation($"Неудачная попытка входа для пользователя: {user.FullName}");

                    return (false, string.Empty);
                }
               
            }
            catch (Exception ex)
            {
                _logger.LogError($"Критическая ошибка при проверке учетных данных: {ex.Message}");
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


        public async Task<UserEntity?> FindUserAsync(string email)
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
