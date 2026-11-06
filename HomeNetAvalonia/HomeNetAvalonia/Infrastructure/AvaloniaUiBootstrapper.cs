using Dapper;
using HomeNet.DI;
using HomeNet.Network.SignalRListeners;
using HomeNetAvalonia.Animators;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetOrm.Helpers;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.DbInfrastructure;
using System;
using System.Threading.Tasks;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        private static bool _isInitialized = false;
        private static bool _isSyncStarted = false;

        public static AppCompositionRoot BuildAvaloniaContainer(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            if (_isInitialized) return AppCompositionRoot.Instance;

            // 1. Базовые мапперы типов Dapper
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // 2. Строим каркас
            AppCompositionRoot.Initialize(mode, apiBaseUrl, sqliteConn);
            _isInitialized = true;

            AppCompositionRoot root = AppCompositionRoot.Instance;

            // ============================================================================
            // 🚌 ПОДПИСКИ НА ЖИЗНЕННЫЙ ЦИКЛ
            // ============================================================================
            root.EventBus.SubscribeAsync<ServerBecameReadyEvent>(OnServerBecameReady);
            root.EventBus.SubscribeAsync<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);
            root.EventBus.SubscribeAsync<IUserVm.UserSignedIn>(OnUserSessionActivated);
            root.EventBus.SubscribeAsync<IUserVm.UserSignedUp>(OnUserSessionActivated);

            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                _ = new CloseWindowAnimator(root.EventBus);
                _ = new MinimizeWindowAnimator(root.EventBus);
                _ = new WidescreenWindowAnimator(root.EventBus);
            }

            // ============================================================================
            // 💥 СТАРТ ИНИЦИАЛИЗАЦИИ БАЗ
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
            root.Logger.LogInfo("[Bootstrapper Старт] Локальные СУБД проверены. Поднимаем сокеты...");

            try
            {
                // 🔥 Подключаем оба листенера (они сами разберутся, какие события слушать)
                await root.UserListener.ConnectAsync();
                await root.MessageListener.ConnectAsync();

                // Защитный таймер на 8 секунд ожидания рукопожатия бэка
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(8));
                    if (!_isSyncStarted)
                    {
                        root.Logger.LogWarning("[SiberNet Engine] ⏳ Превышено время ожидания сервера (8 сек). Переход в Offline Mode.");
                    }
                });
            }
            catch (Exception ex)
            {
                root.Logger.LogError($"[Bootstrapper Старт] Ошибка сокета: {ex.Message}");
            }
        }

        private static async Task OnServerBecameReady(ServerBecameReadyEvent ev)
        {
            if (_isSyncStarted) return;
            _isSyncStarted = true;

            AppCompositionRoot root = AppCompositionRoot.Instance;
            root.Logger.LogInfo("[SiberNet Engine] Сигнал готовности получен бутстраппером! Зажигаем репликацию...");

            try
            {
                await root.OrmEngine.StartAsync();
                root.Logger.LogInfo("[Bootstrapper Старт] ✅ Репликация запущена в штатном режиме.");
            }
            catch (Exception ex)
            {
                root.Logger.LogError($"[Bootstrapper Старт] Критический сбой движка репликации: {ex.Message}");
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
                    // 🔥 Подключаем раздельные листенеры с привязкой к UserId
                    await root.UserListener.ConnectAsync(authenticatedUserId.Value);
                    await root.MessageListener.ConnectAsync(authenticatedUserId.Value);
                }
            }
            catch (Exception ex)
            {
                root.Logger.LogError($"[Bootstrapper] Сбой отложенного старта по сессии: {ex.Message}");
            }
        }
    }
}