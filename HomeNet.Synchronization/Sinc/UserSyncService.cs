using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.Network.ApiClient;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Exeptions;
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

                    try
                    {
                        await _userRepository.InsertAsync(user);
                    }
                    catch (DuplicateEmailException)
                    {
                        _logger.LogDebug($"[UserSyncService] Юзер {user.Email} уже есть в кэше. Пропускаем.");
                    }
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
                        await _apiClient.PushUserAsync(user);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка PUSH: {ex.Message}");
            }
        }

        private void SubscribeEvents()
        {
            // 🔥 Подписываемся на async Task, а не async void
            _eventBus.SubscribeAsync<IUserApiClient.UserRegisteredOnServer>(OnUserRegistered);
            _eventBus.SubscribeAsync<IUserApiClient.UserDeletedOnServer>(OnUserDeleted);
            _eventBus.SubscribeAsync<UserReceivedFromNetwork>(OnUserReceivedFromNetwork);
            _eventBus.SubscribeAsync<UserDeletedFromNetwork>(OnUserDeletedFromNetwork);
        }

        // 🔥 Возвращаем Task, а не void
        private async Task OnUserRegistered(IUserApiClient.UserRegisteredOnServer msg)
        {
            try
            {
                _logger.LogInfo($"[UserSyncService] Сервер подтвердил юзера {msg.User.Id}. Пишем в SQLite.");
                await _userRepository.InsertAsync(msg.User);
            }
            catch (DuplicateEmailException)
            {
                _logger.LogDebug($"[UserSyncService] Юзер {msg.User.Email} уже есть в кэше. Пропускаем.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка при сохранении юзера: {ex.Message}");
            }
        }

        private async Task OnUserDeleted(IUserApiClient.UserDeletedOnServer msg)
        {
            try
            {
                _logger.LogInfo($"[UserSyncService] Сервер удалил юзера {msg.UserId}. Стираем локально.");
                await _userRepository.DeleteByIdAsync(msg.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка удаления: {ex.Message}");
            }
        }

        private async Task OnUserReceivedFromNetwork(UserReceivedFromNetwork msg)
        {
            try
            {
                _logger.LogInfo($"[UserSyncService] Сеть прислала юзера {msg.User.Id}. Пишем в SQLite.");
                await _userRepository.InsertAsync(msg.User);
            }
            catch (DuplicateEmailException)
            {
                _logger.LogDebug($"[UserSyncService] Юзер {msg.User.Email} уже есть в кэше. Пропускаем.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка сохранения: {ex.Message}");
            }
        }

        private async Task OnUserDeletedFromNetwork(UserDeletedFromNetwork msg)
        {
            try
            {
                _logger.LogInfo($"[UserSyncService] Сеть прислала удаление юзера {msg.UserId}. Стираем локально.");
                await _userRepository.DeleteByIdAsync(msg.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка удаления: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // 🔥 Отписываемся так же (EventBus сам разберется)
            _eventBus.UnsubscribeAsync<IUserApiClient.UserRegisteredOnServer>(OnUserRegistered);
            _eventBus.UnsubscribeAsync<IUserApiClient.UserDeletedOnServer>(OnUserDeleted);
            _eventBus.UnsubscribeAsync<UserReceivedFromNetwork>(OnUserReceivedFromNetwork);
            _eventBus.UnsubscribeAsync<UserDeletedFromNetwork>(OnUserDeletedFromNetwork);
        }
    }
} 