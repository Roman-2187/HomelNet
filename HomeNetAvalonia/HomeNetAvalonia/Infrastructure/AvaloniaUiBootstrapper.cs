using Dapper;
using HomeNet.DI;
using HomeNet.Network;
using HomeNetAvalonia.Animators;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Network;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetOrm.Helpers;
using HomeNetOrm.Interfaces.Builders;
using System;
using System.Threading.Tasks;

namespace HomeNetAvalonia.Infrastructure
{
    /// <summary>
    /// Бортовой конфигуратор и регулировщик жизненного цикла SiberNet.
    /// Отвечает за чистый запуск систем, оркестрацию СУБД и активацию сокетов. 🧼
    /// </summary>
    public static class AvaloniaUiBootstrapper
    {
        private static bool _isInitialized = false;
        private static bool _isSyncStarted = false; // Защита от двойного старта репликации

        public static AppCompositionRoot BuildAvaloniaContainer(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            if (_isInitialized)
            {
                return AppCompositionRoot.Instance;
            }

            // 1. Базовые мапперы типов Dapper
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // 2. Строим чистый каркас контейнера Pure DI
            AppCompositionRoot.Initialize(mode, apiBaseUrl, sqliteConn);
            _isInitialized = true;

            AppCompositionRoot root = AppCompositionRoot.Instance;

            // ============================================================================
            // 🚌 ОРКЕСТРАЦИЯ ЖИЗНЕННОГО ЦИКЛА СИСТЕМЫ (ПОДПИСКИ)
            // ============================================================================

            // Системные подписки на готовность слоев, переехавшие из Root
            root.EventBus.SubscribeAsync<ServerBecameReadyEvent>(OnServerBecameReady);
            root.EventBus.SubscribeAsync<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);

            // Родные автобусы авторизации вьюмоделей сессий
            root.EventBus.SubscribeAsync<IUserVm.UserSignedIn>(OnUserSessionActivated);
            root.EventBus.SubscribeAsync<IUserVm.UserSignedUp>(OnUserSessionActivated);

            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                _ = new CloseWindowAnimator(root.EventBus);
                _ = new MinimizeWindowAnimator(root.EventBus); // 🔥 ДОБАВИЛИ СВЕРТЫВАНИЕ
                _ = new WidescreenWindowAnimator(root.EventBus); // 
            }

            // ============================================================================
            // 💥 СТАРТ АСИНХРОННОЙ ИНИЦИАЛИЗАЦИИ БАЗ ДАННЫХ
            // ============================================================================
            Task.Run(async () =>
            {
                await root.DataAccess.DbContextContainer.InitializeAllDatabasesAsync();
            });

            return root;
        }

        private static async Task OnInfrastructureReady(IDbContextContainer.InfrastructureReady ev)
        {
            AppCompositionRoot root = AppCompositionRoot.Instance;
            root.Logger.LogInfo("[Bootstrapper Старт] Локальные СУБД проверены. Поднимаем сокет...");

            try
            {
                await root.SignalRListener.ConnectAsync();

                // Автономный защитный таймер на 8 секунд ожидания рукопожатия бэка
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(8));

                    if (!_isSyncStarted)
                    {
                        root.Logger.LogWarning("[SiberNet Engine] ⏳ Превышено время ожидания сервера (8 сек). Переход в Offline Mode.");
                        root.EventBus.Publish(root, new NetworkStatusChanged(false));
                    }
                });
            }
            catch (Exception ex)
            {
                root.Logger.LogError($"[Bootstrapper Старт] Ошибка сокета: {ex.Message}");
                root.EventBus.Publish(root, new NetworkStatusChanged(false));
            }
        }

        private static async Task OnServerBecameReady(ServerBecameReadyEvent ev)
        {
            if (_isSyncStarted) return;
            _isSyncStarted = true;

            AppCompositionRoot root = AppCompositionRoot.Instance;
            root.Logger.LogInfo("[SiberNet Engine] Сигнал готовности получен бутстраппером! Зажигаем репликацию...");

            root.EventBus.Publish(root, new NetworkStatusChanged(true));

            try
            {
                await root.OrmEngine.StartAsync();
                root.Logger.LogInfo("[Bootstrapper Старт] ✅ Репликация запущена в штатном режиме.");
            }
            catch (Exception ex)
            {
                root.Logger.LogError($"[Bootstrapper Старт] Критический сбой движка репликации: {ex.Message}");
                root.EventBus.Publish(root, new NetworkStatusChanged(false));
            }
        }

        private static async Task OnUserSessionActivated(object eventData)
        {
            AppCompositionRoot root = AppCompositionRoot.Instance;
            root.Logger.LogInfo("[Bootstrapper] Сессия поймана, пробиваем туннель SignalR по UserId...");

            try
            {
                await root.OrmEngine.StartAsync();

                Guid? authenticatedUserId = null;
                if (eventData is IUserVm.UserSignedIn signInMsg) authenticatedUserId = signInMsg.User?.Id;
                else if (eventData is IUserVm.UserSignedUp signUpMsg) authenticatedUserId = signUpMsg.User?.Id;

                if (authenticatedUserId.HasValue)
                {
                    await root.SignalRListener.ConnectAsync(authenticatedUserId.Value);
                }
            }
            catch (Exception ex)
            {
                root.Logger.LogError($"[Bootstrapper] Сбой отложенного старта по сессии: {ex.Message}");
            }
        }
    }
}
