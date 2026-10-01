using System;
using HomeNet.DI;
using HomeNetAvalonia.Infrastructure.Animators;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetPresentation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
       
        public static IServiceProvider BuildAvaloniaContainer(BackendMode mode, string postgresConn, string sqliteConn)
        {
            // 1. Унаследовали чертеж кроссплатформенного бэкенда из Ядра (AppBootstrapper)
            ServiceCollection fullCollection = AppBootstrapper.CreateBackendCollection(mode, postgresConn, sqliteConn);

            // 2. Дописываем провода, которые принадлежат исключительно Авалонии
            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                fullCollection.AddSingleton<CloseWindowAnimator>();
            }

            // 3. Собираем ОДИН монолитный контейнер на всё приложение
            IServiceProvider provider = fullCollection.BuildServiceProvider();


            // 🔥 ШАГ 1: ИНИЦИАЛИЗИРУЕМ БАЗОВЫЙ СТАТИЧЕСКИЙ ЛОКАТОР СЕРВИСОВ ЯДРА
            AppBootstrapper.SetProvider(provider);

            // 🔥 ШАГ 1.5: ЖЕЛЕЗНЫЙ ПРОГРЕВ НАВИГАТОРОВ SIBERNET 🧼
            provider.GetRequiredService<AdminNavigationManager>();
            provider.GetRequiredService<UserNavigationManager>();

            // 🔥 ШАГ 2: БУДИМ МЕНЕДЖЕР ЛОГОВ (Запись на диск активируется)
            provider.GetRequiredService<ILogQueueManager>();

            // Аниматор окон
            provider.GetRequiredService<CloseWindowAnimator>();

            return provider;
        }

    }
}
