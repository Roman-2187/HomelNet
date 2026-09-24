using System;
using HomeNet.DI;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using HomeNetPresentation.ViewModels.AdminViews;
using Microsoft.Extensions.DependencyInjection;
using SiberNet.ConsoleTest; // Твой неймспейс с ConsoleTerminalRenderer

namespace SiberNet.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "SiberNet v1.0 - Core Console Terminal";

            // 1. 🔥 ШАГ 1: Собираем чистый бэкенд через твой универсальный Build метод
            // Он автоматически зарегистрирует IEventBus, LogQueueManager и TerminalLogsViewModel
            string postgresConn = "Host=localhost;Database=SiberNet;Username=postgres;Password=root";
            string sqliteConn = "Data Source=SiberNetFallback.db";

            IServiceProvider provider = AppBootstrapper.Build(BackendMode.Local, postgresConn, sqliteConn);

            // 2. 🔥 ШАГ 2: Вытаскиваем готовую универсальную TerminalLogsViewModel из контейнера
            var terminalVm = provider.GetRequiredService<TerminalLogsViewModel>();

            // 3. 🔥 ШАГ 3: МАГИЯ АВТО-ОБНОВЛЕНИЯ КОНСОЛИ
            // Подписываемся на изменение коллекции Logs. 
            // Как только в двухмерный массив падает буква или строка, консоль мгновенно перерисовывается!
            terminalVm.Logs.CollectionChanged += (s, e) =>
            {
                // Вызываем наш свитч-рендерер для консоли
                ConsoleTerminalRenderer.PrintToConsole(terminalVm);
            };

            // Имитируем работу системы, чтобы консоль не закрывалась сразу
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Ядро SiberNet запущено в консольном режиме. Ожидание системных событий...");

            // Чтобы проверить, можешь прямо здесь сгенерировать тестовый лог в шину
            var eventBus = provider.GetRequiredService<IEventBus>();
             eventBus.Publish(null, new ILogQueueManager.LogMessageReceived("Проверка связи...", LogLevel.Information, "Core", true));


            eventBus.Publish(null, new ILogQueueManager.LogMessageReceived("Все плохо...", LogLevel.Error, "Core", true));

            Console.ReadLine(); // Держим консоль открытой
        }
    }
}
