
using HomeNet.DI;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetPresentation.Services; // Добавили пространство имен для менеджеров навигации
using HomeNetPresentation.ViewModels.AdminViews;
using Microsoft.Extensions.DependencyInjection;
using SiberNet.UI.Infrastructure.Animators;
using System;

namespace SiberNet.UI.Infrastructure
{
    public static class WpfUiBootstrapper
    {
        /// <summary>
        /// 🔥 РАСШИРЯЕТ И АКТИВИРУЕТ: Забирает чертеж бэкенда из Ядра,
        /// дописывает туда WPF-аниматоры и цементирует в один общий провайдер.
        /// </summary>
        public static IServiceProvider BuildWpfContainer(BackendMode mode, string postgresConn, string sqliteConn)
        {
            // 1. Унаследовали чертеж кроссплатформенного бэкенда из Ядра
            ServiceCollection fullCollection = AppBootstrapper.CreateBackendCollection(mode, postgresConn, sqliteConn);

            // 2. Дописываем провода, которые принадлежат исключительно WPF
            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                fullCollection.AddSingleton<CloseWindowAnimator>();
                fullCollection.AddSingleton<ResizeWindowAnimator>();
                fullCollection.AddSingleton<GlobalLoggerWindowAnimator>();
            }

            // 3. Собираем ОДИН монолитный контейнер на всё приложение
            IServiceProvider provider = fullCollection.BuildServiceProvider();

            // 🔥 ШАГ 1: ИНИЦИАЛИЗИРУЕМ БАЗОВЫЙ ЛОКАТОР СЕРВИСОВ
            AppBootstrapper.SetProvider(provider);

            // 🔥 ШАГ 1.5: ЖЕЛЕЗНЫЙ ПРОГРЕВ НАВИГАТОРОВ SIBERNET 🧼
            // Вытаскиваем ленивые синглтоны из контейнера, чтобы они сели на шину событий ДО кликов пользователя!
            provider.GetRequiredService<AdminNavigationManager>();
            provider.GetRequiredService<UserNavigationManager>();

            // 🔥 ШАГ 2: БУДИМ МЕНЕДЖЕР ЛОГОВ (Запись на диск активируется)
            provider.GetRequiredService<ILogQueueManager>();

            // 🔥 ХИТРЫЙ ПЕРЕХВАТ ДЛЯ WPF:
            // Вытаскиваем чистую вьюмодель и говорим движку WPF автоматически синхронизировать её потоки!
            var terminalVm = provider.GetRequiredService<TerminalLogsViewModel>();
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(terminalVm.Logs, new object());

            // 4. 🔥 АКТИВИРУЕМ АНИМАТОРЫ ОКНА
            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                provider.GetRequiredService<CloseWindowAnimator>();
                provider.GetRequiredService<ResizeWindowAnimator>();
                provider.GetRequiredService<GlobalLoggerWindowAnimator>();
            }

            return provider;
        }
    }
}
