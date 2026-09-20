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
            }
            else
            {
                // Сюда можно дописать фейковые Mock-заглушки аниматоров для UI тестов, если понадобятся
            }

            // 3. Собираем ОДИН монолитный контейнер на всё приложение
            IServiceProvider provider = fullCollection.BuildServiceProvider();



            // 🔥 ШАГ 1: ПЕРВЫМ ДЕЛОМ БУДИМ МЕНЕДЖЕР ЛОГОВ! 
            // Это мгновенно выполнит фабрику в AppBootstrapper и прикрутит запись на диск!
            provider.GetRequiredService<ILogQueueManager>();

          

            // 🔥 ВОТ ОНА — ЗАПЛАТКА ВЕКА! Говорим бэкенд-локатору использовать наш общий куб!
            AppBootstrapper.SetProvider(provider);

            // 4. 🔥 АКТИВИРУЕМ АНИМАТОРЫ: DI-контейнер принудительно создает экземпляры синглтонов,
            // за счет чего их конструкторы просыпаются и намертво цепляются к кроссплатформенной шине событий!
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
