using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events; // 🔥 Подключаем автобус событий
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;

namespace HomeNetServices.Services.Identity
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus; // 🔥 Теперь сервис сам может оповещать систему

        // 🧠 Наш локальный кэш пользователей в оперативной памяти
        private List<UserEntity>? _cachedUsers;
        private readonly object _lock = new(); // Для потокобезопасности кэша

        public UserService(IUserRepository repo, ILogger logger, IEventBus eventBus)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
          
            try
            {
                // Если кэш пуст — только тогда идем в физическую СУБД SQLite 🚀
                if (_cachedUsers == null)
                {
                    _logger.LogInformation("Кэш пуст. Выполняется первичный запрос к СУБД...");
                    var users = await _repo.GetAllAsync()
                        ?? throw new InvalidOperationException("Репозиторий вернул null");

                    lock (_lock)
                    {
                        _cachedUsers = users;
                    }
                    _logger.LogInformation($"Получено и закэшировано {_cachedUsers.Count} пользователей.");
                }
                else
                {
                    _logger.LogDebug($"[КЭШ В ПАМЯТИ]: Возвращено {_cachedUsers.Count} пользователей без обращения к СУБД.");
                }

                // Возвращаем копию списка, чтобы UI-слой случайно не попортил внутренний кэш сервиса
                lock (_lock)
                {
                    return _cachedUsers.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при получении пользователей", ex.Message);
                throw;
            }
        }

        public async Task InsertUserAsync(UserEntity user)
        {
            try
            {
                // 1. Сначала пишем в базу данных
                await _repo.InsertUserAsync(user);
                _logger.LogDebug($"Пользователь {user.FirstName} успешно вставлен в БД.");

                // 2. Моментально обновляем кэш в оперативной памяти 🧠
                if (_cachedUsers != null)
                {
                    lock (_lock)
                    {
                        _cachedUsers.Add(user);
                    }
                }

                // 3. Пинаем автобус, чтобы все открытые UI-окна тут же добавили его на экраны!
                _eventBus.Publish(this, new IUsersTableViewModel.Added(user));
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
                // 1. Сначала удаляем из физической СУБД
                await _repo.DeleteByIdAsync(userId);
                _logger.LogInformation($"Пользователь с ID {userId} удалён из БД.");

                // 2. Моментально чистим кэш в памяти 🧠
                if (_cachedUsers != null)
                {
                    lock (_lock)
                    {
                        var userToRemove = _cachedUsers.FirstOrDefault(u => u.Id == userId);
                        if (userToRemove != null)
                        {
                            _cachedUsers.Remove(userToRemove);
                        }
                    }
                }

                // 3. Пинаем автобус: "Народ, этого юзера больше нет!" 📢
                // Все вьюшки (таблица, контакты, удаление) сами выкинут его из UI без единого запроса к базе!
                _eventBus.Publish(this, new IDeleteUserViewModel.Deleted(userId));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Попытка удалить несуществующего пользователя", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Критическая ошибка удаления ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<UserEntity?> GetByIdAsync(int userId)
        {
            // Если кэш уже подгружен — ищем в памяти мгновенно!
            if (_cachedUsers != null)
            {
                lock (_lock)
                {
                    return _cachedUsers.FirstOrDefault(u => u.Id == userId);
                }
            }

            // Иначе падаем на стандартный поход в СУБД
            return await _repo.GetByIdAsync(userId);
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            if (_cachedUsers != null)
            {
                lock (_lock)
                {
                    return _cachedUsers.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                }
            }

            return await _repo.GetByEmailAsync(email);
        }
    }
}
