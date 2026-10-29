using System;
using System.Threading.Tasks;
using Dapper;
using HomeNet.DI;
using HomeNetAvalonia.Animators;
using HomeNetCore.Extensions;
using HomeNetCore.Enums;
using HomeNetOrm.Helpers;
using HomeNetCore.Interfaces.ViewModels;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        private static bool _isInitialized = false;

        public static AppCompositionRoot BuildAvaloniaContainer(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            if (_isInitialized)
            {
                return AppCompositionRoot.Instance;
            }

            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            AppCompositionRoot.Initialize(mode, apiBaseUrl, sqliteConn);
            _isInitialized = true;

            AppCompositionRoot root = AppCompositionRoot.Instance;

            // ============================================================================
            // 🚌 ЛОВИМ РОДНЫЕ АВТОБУСЫ ВЬЮМОДЕЛЕЙ
            // Подписываемся на события, которые вьюмодели ПРЕКРАСНО знают и сами пускают в шину!
            // ============================================================================
            root.EventBus.SubscribeAsync<IUserVm.UserSignedIn>(OnUserSessionActivated);
            root.EventBus.SubscribeAsync<IUserVm.UserSignedUp>(OnUserSessionActivated);

            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                var animator = new CloseWindowAnimator(root.EventBus);
                var widescreenAnimator = new WidescreenWindowAnimator(root.EventBus);
            }

            return root;
        }

        private static async Task OnUserSessionActivated(object eventData)
        {
            AppCompositionRoot root = AppCompositionRoot.Instance;
            root.Logger.LogInfo("[Avalonia Bootstrapper] Сигнал авторизации пойман. Будим OrmEngine...");

            try
            {
                // 1. Включаем ОРМ-движок. Он мгновенно настраивает свои внутренние подписки и сам сканирует базу!
                await root.OrmEngine.StartAsync();
                root.Logger.LogInfo("[Avalonia Bootstrapper] ✅ Репликатор OrmEngine успешно разбужен.");

                // ✂️ УДАЛЕНО: await Task.Delay(10); — костыль уничтожен за ненадобностью!
            }
            catch (Exception ex)
            {
                root.Logger.LogError($"[Avalonia Bootstrapper] ❌ Ошибка отложенного старта репликации: {ex.Message}");
            }
        }

    }
}
