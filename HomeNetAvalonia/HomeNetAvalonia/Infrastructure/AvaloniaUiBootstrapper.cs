using Dapper;
using HomeNet.DI;
using HomeNetAvalonia.Animators;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetOrm.Helpers;
using HomeNetOrm.Interfaces;
using HomeNetPresentation.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        public static IServiceProvider BuildAvaloniaContainer(BackendMode mode, string postgresConn, string sqliteConn)
        {

            // 🔥 ЖЕЛЕЗОБЕТОННЫЙ ХЕНДЛЕР: Обучаем Dapper читать DateTimeOffset из строк SQLite без падений!
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());

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
            // Будим координатор синхронизации баз данных SiberNet
            provider.GetRequiredService<ISiberNetSyncCoordinator>();



            return provider;
        }
    }
}
