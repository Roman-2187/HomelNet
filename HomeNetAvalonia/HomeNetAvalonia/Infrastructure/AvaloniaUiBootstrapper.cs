using System;
using HomeNet.DI;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;
using HomeNetPresentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        /// <summary>
        /// 🔥 РАСШИРЯЕТ И АКТИВИРУЕТ: Забирает кроссплатформенный чертеж бэкенда из Ядра,
        /// дописывает туда Авалония-сервисы и цементирует в один общий провайдер.
        /// </summary>
        public static IServiceProvider BuildAvaloniaContainer(BackendMode mode, string postgresConn, string sqliteConn)
        {
            // 1. Унаследовали чертеж кроссплатформенного бэкенда из Ядра (AppBootstrapper)
            ServiceCollection fullCollection = AppBootstrapper.CreateBackendCollection(mode, postgresConn, sqliteConn);
// Внутри метода BuildAvaloniaContainer:
            
            // 2. Дописываем провода, которые принадлежат исключительно Авалонии
            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
               fullCollection.AddSingleton<HomeNetAvalonia.Infrastructure.Animators.CloseWindowAnimator>(); // Сюда мы позже пропишем Авалония-аниматоры окон, когда перепишем их с WPF-потоков
                // fullCollection.AddSingleton<AvaloniaCloseWindowAnimator>();
            }

            // 3. Собираем ОДИН монолитный контейнер на всё приложение
            IServiceProvider provider = fullCollection.BuildServiceProvider();

            // 🔥 ШАГ 1: ИНИЦИАЛИЗИРУЕМ БАЗОВЫЙ СТАТИЧЕСКИЙ ЛОКАТОР СЕРВИСОВ ЯДРА
            AppBootstrapper.SetProvider(provider);

            // 🔥 ШАГ 1.5: ЖЕЛЕЗНЫЙ ПРОГРЕВ НАВИГАТОРОВ SIBERNET 🧼
            // Вытаскиваем ленивые синглтоны из контейнера, чтобы они сели на шину событий ДО кликов пользователя!
            provider.GetRequiredService<AdminNavigationManager>();
            provider.GetRequiredService<UserNavigationManager>();

            // 🔥 ШАГ 2: БУДИМ МЕНЕДЖЕР ЛОГОВ (Запись на диск активируется)
            provider.GetRequiredService<ILogQueueManager>();

            // В Авалонии встроенная поддержка многопоточных коллекций работает из коробки,
            // поэтому костыль с EnableCollectionSynchronization для логов нам здесь больше не нужен!

            

            // Чуть ниже, после сборки provider = fullCollection.BuildServiceProvider():
            provider.GetRequiredService<HomeNetAvalonia.Infrastructure.Animators.CloseWindowAnimator>();

            return provider;
        }
    }
}
