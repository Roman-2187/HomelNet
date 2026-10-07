using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Models;
using HomeNetOrm.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeNetServices.Services.Identity
{
    public class FriendService : IFriendService
    {
        private readonly IFriendRepository _friendRepository;
        private readonly ILogger _logger;

        public FriendService(IFriendRepository friendRepository, ILogger logger)
        {
            _friendRepository = friendRepository ?? throw new ArgumentNullException(nameof(friendRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // 🤝 Добавить пользователя в контакты
        public async Task<bool> AddFriendToUserAsync(Guid userId, Guid friendId)
        {
            try
            {
                // Инициализируем модель с новыми Guid-идентификаторами сущностей
                var friendLink = new FriendEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FriendId = friendId,
                    IsSynced = 0, // По умолчанию офлайн, репозиторий сам выставит 1 если Postgres жив
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                bool success = await _friendRepository.AddFriendAsync(friendLink);

                if (success)
                {
                    _logger.LogInfo($"[Контакты] Пользователь с GUID {userId} успешно добавил в контакты GUID {friendId}.");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Ошибка Контактов] Не удалось добавить друга: {ex.Message}");
                return false;
            }
        }

        // 👥 Загрузить список контактов из базы БЕЗ ЛАГОВ СЕТИ
        public async Task<IEnumerable<UserEntity>> GetFriendsListAsync(Guid userId)
        {
            try
            {
                var friends = await _friendRepository.GetFriendsForUserAsync(userId);
                _logger.LogInfo($"[Контакты] Список друзей для пользователя с GUID {userId} успешно извлечён из SQLite.");
                return friends;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Ошибка Контактов] Ошибка загрузки списка друзей: {ex.Message}");
                return new List<UserEntity>();
            }
        }
    }
}
