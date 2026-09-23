
        using System;
using HomeNet.DI;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.OutputLogging;
using Microsoft.Extensions.DependencyInjection;
using SiberNet.UI.Infrastructure.Animators;

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

                    // ✂️ СТРОКУ С РЕГИСТРАЦИЕЙ WpfLogAnimator УДАЛИЛИ ОТСЮДА НАФИГ!
                }

                // 3. Собираем ОДИН монолитный контейнер на всё приложение
                IServiceProvider provider = fullCollection.BuildServiceProvider();

                // 🔥 ШАГ 1: ИНИЦИАЛИЗИРУЕМ БАЗОВЫЙ ЛОКАТОР СЕРВИСОВ
                AppBootstrapper.SetProvider(provider);

            // 🔥 ШАГ 2: БУДИМ МЕНЕДЖЕР ЛОГОВ (Запись на диск активируется)
            // provider.GetRequiredService<ILogQueueManager>();

            provider.GetRequiredService<ILogQueueManager>();

            // ✂️ ШАГ 3 С КИКСТАРТОМ WpfLogAnimator ТОЖЕ ПОЛНОСТЬЮ УДАЛИЛИ!
            // Твой LogUiAnimation сам подцепится к шине событий при открытии вкладки логов.

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




