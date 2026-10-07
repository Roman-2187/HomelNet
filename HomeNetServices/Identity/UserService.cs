using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;

namespace HomeNetServices.Services.Identity
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;

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
                _logger.LogDebug("Запрос списка пользователей напрямую из СУБД...");
                var users = await _repo.GetAllAsync()
                    ?? throw new InvalidOperationException("Репозиторий вернул null");

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
                // 1. Пишем напрямую в базу данных
                await _repo.InsertUserAsync(user);
                _logger.LogDebug($"Пользователь {user.FirstName} успешно записан в БД.");

                // 2. Пинаем автобус, чтобы все UI-окна добавили его на экраны
                _eventBus.Publish(this, new IUsersTableVm.Added(user));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при добавлении пользователя в БД: {ex.Message}");
                throw;
            }
        }

        // 🎯 ТЕПЕРЬ ТУТ ЧЕСТНЫЙ Guid вместо int
        public async Task DeleteByIdAsync(Guid userId)
        {
            try
            {
                // 1. Удаляем напрямую из СУБД (коммутатор контейнера сам разберётся, куда слать запрос)
                await _repo.DeleteByIdAsync(userId);
                _logger.LogInfo($"Пользователь с GUID {userId} успешно удалён из БД.");

                // 2. Публикуем событие в автобус — вьюшки выкинут юзера из списков UI
                _eventBus.Publish(this, new IDeleteUserVm.Deleted(userId));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning($"Попытка удалить несуществующего пользователя с GUID {userId}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Критическая ошибка удаления GUID {userId}: {ex.Message}");
                throw;
            }
        }

        // 🎯 ТЕПЕРЬ ТУТ ЧЕСТНЫЙ Guid вместо int
        public async Task<UserEntity?> GetByIdAsync(Guid userId)
        {
            _logger.LogDebug($"Точечный запрос пользователя по GUID {userId} напрямую из СУБД...");
            return await _repo.GetByIdAsync(userId);
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
           
            return await _repo.GetByEmailAsync(email);
        }
    }
}
