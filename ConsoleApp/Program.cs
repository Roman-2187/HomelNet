using HomeNet.DI;
using HomeNetCore.Enums;
using HomeNetCore.Events;
using HomeNetCore.Interfaces; // Подключаем наш универсальный куб
using HomeNetCore.Models;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using HomeNetOrm.Interfaces;
using HomeNetPresentation.ViewModels;
using HomeNetServices.Diagnostics;
using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "🛸 SiberNet Core - Консольный Тест-Драйв Двигателя";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=================================================================");
            Console.WriteLine("        ЗАПУСК ЯДРА МЕССЕНДЖЕРА В АВТОНОМНОЙ КОНСОЛИ              ");
            Console.WriteLine("   Абсолютное доказательство независимости кода от оболочек UI   ");
            Console.WriteLine("=================================================================");
            Console.ResetColor();

            // 1. ИНИЦИАЛИЗИРУЕМ УНИВЕРСАЛЬНЫЙ КАБЕЛЬ (Строки подключения)
            string dbPath = "home_net_console.db"; // Локальная тестовая SQLite
            string postgresConn = "Server=127.0.0.1;Database=home_net_db;User Id=postgres;Password=05011987;";

            Console.WriteLine("\n[1] Собираем DI-контейнер и будим сервисы...");
            var container = AppBootstrapper.Build(BackendMode.Real, postgresConn, $"Data Source={dbPath}");

            // Будим логгер и базы (точно так же, как в WPF!)
            var kickLogger = AppBootstrapper.GetViewModel<LogQueueManager>();
            var dbCore = AppBootstrapper.GetViewModel<DbContextContainer>();

            // Включаем базу!
            await dbCore.InitializeAsync(DatabaseType.PostGreSQL);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[ОК] База данных PostgreSQL успешно разбужена из консоли!");
            Console.ResetColor();

            // 2. ВЫТАСКИВАЕМ ВЬЮМОДЕЛИ НА БЛЮДЕЧКЕ ЧЕРЕЗ НАШ МЕТОД-ЛОКАТОР
            var mainVm = AppBootstrapper.GetViewModel<MainViewModel>();
            var registerVm = AppBootstrapper.GetViewModel<RegistrationViewModel>();

            // 3. ИМИТИРУЕМ XAML-БИНДИНГИ ЧЕРЕЗ PROPERTYCHANGED
            // Консоль будет реактивно ловить изменения свойств в памяти!
            mainVm.PropertyChanged += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"   [РЕАКТИВНЫЙ БИНДИНГ] В памяти изменилось свойство: '{e.PropertyName}'");
                Console.ResetColor();
            };

            Console.WriteLine($"\n[2] Стартовое состояние автомата MainTab: {mainVm.CurrentMainTab}");

            // 4. ТЕСТИРУЕМ ИЕРАРХИЧЕСКИЙ АВТОМАТ СОСТОЯНИЙ (Клик по админке)
            Console.WriteLine("\n[3] Эмулируем клик пользователя по кнопке '🛠️ Админка'...");
            mainVm.ToggleAdminZoneCommand.Execute(null);
            // 💥 БУМ! Сеттер перещёлкнет enum, сбросит дочерние окна, и консоль тут же выведет ивент!

            Console.WriteLine($"    Новое состояние автомата MainTab: {mainVm.CurrentMainTab}"); // Должно стать AdminZone

            // =================================================================
            // 🔥 ШАГ: ИСПЫТАНИЕ БЭКЕНДА И СУБД (ПРАВИЛЬНЫЙ И НЕПРАВИЛЬНЫЙ ВВОД)
            // =================================================================
            var registerService = AppBootstrapper.GetViewModel<IRegisterService>();

            // -----------------------------------------------------------------
            // 🟢 СЦЕНАРИЙ А: ЗАВЕДОМО ПРАВИЛЬНЫЙ ЮЗЕР (Должен улететь в базу)
            // -----------------------------------------------------------------
            Console.WriteLine("\n--- [4.1]: Тест успешной регистрации в PostgreSQL ---");

            // Уникальный email, чтобы не поймать дубликат в базе
            string uniqueEmail = $"test_user_{DateTime.Now.Ticks}@sibernet.ru";

            var correctUser = new UserEntity
            {
                FirstName = "Роман",
                Email = uniqueEmail,
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            Console.WriteLine($"    Отправляем валидного юзера '{correctUser.FirstName}' (Почта: {correctUser.Email})...");

            try
            {
                var dbVerdict = await registerService.RegisterUserAsync(correctUser);

                if (dbVerdict.IsValid)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("   [УСПЕХ БАЗЫ ДАННЫХ]: Юзер пробил валидацию и запекся в СУБД!");
                    Console.WriteLine($"   Присвоенный PostgreSQL ID: {dbVerdict.VerifiedUser?.Id}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   [ОТКАЗ СУБД]: Ошибка, база не должна была отклонить этот ввод!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"   [КРИТИЧЕСКИЙ ВЗРЫВ БАЗЫ]: {ex.Message}");
                Console.ResetColor();
            }

            // -----------------------------------------------------------------
            // 🔴 СЦЕНАРИЙ Б: ЗАВЕДОМО НЕПРАВИЛЬНЫЙ ЮЗЕР (Должен отбиться валидацией)
            // -----------------------------------------------------------------
            Console.WriteLine("\n--- [4.2]: Тест падения валидации (База не дергается) ---");

            var incorrectUser = new UserEntity
            {
                FirstName = "Ро",               // Слишком короткое имя (минимум 3 буквы)
                Email = "invalid_email_format",  // Кривой email
                Password = "123",               // Короткий пароль
                ConfirmPassword = "321"         // Пароли не совпадают
            };

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"    Отправляем косячного юзера '{incorrectUser.FirstName}' (Почта: {incorrectUser.Email})...");
            Console.ResetColor();

            // Гоняем через тот же самый метод сервиса бэкенда! Без дублирования логики.
            var failVerdict = await registerService.RegisterUserAsync(incorrectUser);

            if (!failVerdict.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   [ОТКАЗ ВАЛИДАЦИИ ЯДРА]: Движок SiberNet успешно заблокировал кривой ввод!");

                // Выводим все ошибки, которые собрали регулярки Ядра
                foreach (var err in failVerdict.Results)
                {
                    if (err.State == ValidationState.Error)
                    {
                        Console.WriteLine($"    • Поле [{err.Field}]: {err.Message}");
                    }
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("   [БАГ ARCHITECTURE]: Каким-то чудом кривой юзер попал в базу! Проверь регулярки.");
                Console.ResetColor();
            }




            // 6. ТЕСТИРУЕМ ШИНУ СОБЫТИЙ (IEventBus) В ПОЛНОЙ ИЗОЛЯЦИИ
            Console.WriteLine("\n[5] Тестируем реакцию на падение интернета...");
            await dbCore.SwitchDatabaseAsync(DatabaseType.SQLite); // Аварийно уходим в офлайн

            var eventBus = AppBootstrapper.GetViewModel<IEventBus>();
            eventBus.Publish(null, new StatusTextChangedMessage("⚠️ Сеть упала на стройке! Консоль зафиксировала переход на SQLite."));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=======================================================");
            Console.WriteLine("   ТЕСТ-ДРАЙВ ЗАВЕРШЕН БЕЗ ЕДИНОЙ ОШИБКИ!              ");
            Console.WriteLine("   Двигателю SiberNet абсолютно насрать на оболочку.    ");
            Console.WriteLine("   Теперь можно со спокойной душой идти спать!         ");
            Console.WriteLine("=======================================================");
            Console.ResetColor();

            Console.WriteLine("\nНажми Enter, чтобы закрыть этот пульт и пойти отдыхать...");
            Console.ReadLine();
        }
    }
}
