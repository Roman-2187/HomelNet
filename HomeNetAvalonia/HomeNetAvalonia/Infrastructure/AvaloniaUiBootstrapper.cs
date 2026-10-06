using HomeNet.DI;
using HomeNetAvalonia.Animators;
using HomeNetAvalonia.Controls;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels.AdminViews; // 🔥 ДОБАВЛЯЕМ: Импорт пространства имен твоей вьюмодели
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        public static IServiceProvider BuildAvaloniaContainer(BackendMode mode, string postgresConn, string sqliteConn)
        {
            // 1. Унаследовали чертеж кроссплатформенного бэкенда из Ядра
            ServiceCollection fullCollection = AppBootstrapper.CreateBackendCollection(mode, postgresConn, sqliteConn);

            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                fullCollection.AddSingleton<CloseWindowAnimator>();
            }

            // 2. Собираем ОДИН монолитный контейнер
            IServiceProvider provider = fullCollection.BuildServiceProvider();

            // 🔥 ШАГ 1: ИНИЦИАЛИЗИРУЕМ БАЗОВЫЙ ЛОКАТОР СЕРВИСОВ ЯДРА
            AppBootstrapper.SetProvider(provider);

            // 🔥 ШАГ 2: ПРОГРЕВ НАВИГАТОРОВ (Как в WPF!)
            provider.GetRequiredService<AdminNavigationManager>();
            provider.GetRequiredService<UserNavigationManager>();

            // 🔥 ШАГ 3: БУДИМ МЕНЕДЖЕР ЛОГОВ (Как в WPF!)
            provider.GetRequiredService<ILogQueueManager>();

            // Аниматор окон
            provider.GetRequiredService<CloseWindowAnimator>();


            return provider;
        }
    }
}
