using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Network.ApiClient;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNet.Synchronization.Sinc
{
    public class UserSyncService : ISyncCoordinator, IDisposable
    {
        private readonly IUserRepo _userRepository;
        private readonly IUserApiClient _apiClient;
        private readonly IEventBus _eventBus;
        private readonly ICastomLogger _logger;
        private bool _isInitialSyncExecuted;

        public UserSyncService(
            IUserRepo userRepository,
            IUserApiClient apiClient,
            IEventBus eventBus,
            ICastomLogger logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeEvents();
        }

        // =========================================================================
        // 📥 СТАРТОВАЯ СИНХРОНИЗАЦИЯ (PULL/PUSH)
        // =========================================================================
        public async Task StartAsync()
        {
            if (_isInitialSyncExecuted) return;
            _isInitialSyncExecuted = true;

            _logger.LogInfo("=== [UserSyncService] ЗАПУСК СТАРТОВОЙ СИНХРОНИЗАЦИИ ===");

            await PullAsync();
            await PushAsync();

            _logger.LogInfo("=== [UserSyncService] СТАРТОВАЯ СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА ===");
        }

        private async Task PullAsync()
        {
            try
            {
                var freshUsers = await _apiClient.FetchFreshUsersAsync(Guid.Empty, DateTimeOffset.MinValue);
                foreach (var user in freshUsers)
                {
                    user.IsSynced = 1;
                    await _userRepository.InsertAsync(user); // Репозиторий крикнет Inserted
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка PULL: {ex.Message}");
            }
        }

        private async Task PushAsync()
        {
            try
            {
                var allUsers = await _userRepository.GetAllAsync();
                var pending = allUsers.Where(u => u.IsSynced == 0).ToList();

                if (pending.Count > 0)
                {
                    _logger.LogWarning($"[UserSyncService] Обнаружено {pending.Count} неотправленных юзеров. Инициируем PUSH.");
                    foreach (var user in pending)
                    {
                        // ApiClient сам опубликует факт, координатор (мы) его поймает
                        await _apiClient.PushUserAsync(user);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка PUSH: {ex.Message}");
            }
        }

        // =========================================================================
        // 🎧 РЕАКЦИЯ НА СОБЫТИЯ
        // =========================================================================
        private void SubscribeEvents()
        {
            // Факты от API (успешная регистрация/удаление на сервере)
            _eventBus.Subscribe<IUserApiClient.UserRegisteredOnServer>(OnUserRegistered);
            _eventBus.Subscribe<IUserApiClient.UserDeletedOnServer>(OnUserDeleted);

            // Факты от SignalR (сеть прислала данные)
            _eventBus.Subscribe<UserReceivedFromNetwork>(OnUserReceivedFromNetwork);
            _eventBus.Subscribe<UserDeletedFromNetwork>(OnUserDeletedFromNetwork);
        }

        private async void OnUserRegistered(IUserApiClient.UserRegisteredOnServer msg)
        {
            _logger.LogInfo($"[UserSyncService] Сервер подтвердил юзера {msg.User.Id}. Пишем в SQLite.");
            await _userRepository.InsertAsync(msg.User);
        }

        private async void OnUserDeleted(IUserApiClient.UserDeletedOnServer msg)
        {
            _logger.LogInfo($"[UserSyncService] Сервер удалил юзера {msg.UserId}. Стираем локально.");
            await _userRepository.DeleteByIdAsync(msg.UserId);
        }

        // 🔥 Обработчики для сети
        private async void OnUserReceivedFromNetwork(UserReceivedFromNetwork msg)
        {
            _logger.LogInfo($"[UserSyncService] Сеть прислала юзера {msg.User.Id}. Пишем в SQLite.");
            await _userRepository.InsertAsync(msg.User);
        }

        private async void OnUserDeletedFromNetwork(UserDeletedFromNetwork msg)
        {
            _logger.LogInfo($"[UserSyncService] Сеть прислала удаление юзера {msg.UserId}. Стираем локально.");
            await _userRepository.DeleteByIdAsync(msg.UserId);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IUserApiClient.UserRegisteredOnServer>(OnUserRegistered);
            _eventBus.Unsubscribe<IUserApiClient.UserDeletedOnServer>(OnUserDeleted);
            _eventBus.Unsubscribe<UserReceivedFromNetwork>(OnUserReceivedFromNetwork);
            _eventBus.Unsubscribe<UserDeletedFromNetwork>(OnUserDeletedFromNetwork);
        }
    }
}