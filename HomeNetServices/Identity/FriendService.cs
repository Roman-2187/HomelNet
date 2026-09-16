using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using HomeNetOrm.Repositories;

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
        public async Task<bool> AddFriendToUserAsync(int userId, int friendId)
        {
            try
            {
                var friendLink = new FriendEntity { UserId = userId, FriendId = friendId };
                bool success = await _friendRepository.AddFriendAsync(friendLink);

                if (success)
                {
                    _logger.LogInformation($"[Контакты] Пользователь ID {userId} добавил в контакты ID {friendId}.");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Ошибка Контактов] Не удалось добавить друга: {ex.Message}");
                return false;
            }
        }

        // 👥 Загрузить список контактов из базы
        public async Task<IEnumerable<UserEntity>> GetFriendsListAsync(int userId)
        {
            try
            {
                var friends = await _friendRepository.GetFriendsForUserAsync(userId);
                _logger.LogInformation($"[Контакты] Список друзей для пользователя ID {userId} успешно извлечён.");
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
