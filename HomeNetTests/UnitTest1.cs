using HomeNetCore.Enums;
using HomeNetCore.Interfaces;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using HomeNetPresentation.ViewModels;
using HomeNetServices.Routing;

namespace HomeNet.Tests
{
    public class DbContextTests
    {
        // Наш тестовый логгер-заглушка, чтобы тест ни от чего не зависел 🧼
        private class TestLogger : ILogger
        {
            // 🔥 Дописываем обязательный метод интерфейса, чтобы компилятор отстал!
            public void SetOutput(Action<string, LogLevel> output)
            {
                // Для теста здесь ничего делать не нужно, просто пустые скобки! 🧼🛸
            }

            public void Log(LogLevel level, string message, string memberName = "", string filePath = "", int lineNumber = 0, params object[] args)
            {
                // Пишет логи прямо в окно отладки тестов Visual Studio!
                System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
            }
        }

        [Fact] // 🔥 Главный атрибут xUnit! Говорит Студии: "Это тестовый метод!"
        public async Task InitializeAsync_ShouldSuccessfullySwitchToSqlite()
        {       
            // 1. АРРАНЖ (Подготовка данных)
            string fakePostgres = "Server=127.0.0.1;Database=fake;User Id=postgres;Password=05011987;";
            string testSqlite = "Data Source=test_home_net.db"; // Делаем отдельную тестовую базу!

            var logger = new TestLogger();
            var container = new DbContextContainer(fakePostgres, testSqlite, logger);

            // 2. АКТ (Выполняем действие, которое вчера вешало рантайм!)
            // Пробуем запустить форсирование на SQLite
            Exception? caughtException = null;
            try
            {
                await container.InitializeAsync(DatabaseType.SQLite);
            }
            catch (Exception ex)
            {
                caughtException = ex; // Запоминаем ошибку, если она грохнулась
            }

            // 3. АССЕРТ (Проверка результата)
            // Мы утверждаем, что никаких ошибок быть не должно, а тип СУБД обязан стать SQLite! 👍
            Assert.Null(caughtException);
            Assert.Equal(DatabaseType.SQLite, container.CurrentType);

            // Чистим за собой соединение
            await container.DisposeAsync();
        }



        [Fact] // 🔥 Наш радар для проверки ВьюМодели!
        public void MainViewModel_Constructor_ShouldInitializeSuccessfully()
        {
            // 1. АРРАНЖ: Создаем чистые заглушки для конструктора, как это делает DI 🧼
            ILogger testLogger = new TestLogger();
            IEventBus testEventBus = new EventBus();
            // (Или используй свою реализацию EventBus из проекта)

            Exception? caughtException = null;
            MainViewModel? mainVm = null;

            // 2. АКТ: Пробуем создать экземпляр вьюмодели, пиная логи на строках 24-27! 🦾
            try
            {
                mainVm = new MainViewModel(testLogger, testEventBus);
            }
            catch (Exception ex)
            {
                caughtException = ex; // Ловим диверсанта, если конструктор упадет!
            }

            // 3. АССЕРТ: Проверяем, что объект создан, ошибок нет, а флаги встали по дефолту! 🛸🛡️
            Assert.Null(caughtException);
            Assert.NotNull(mainVm);
            Assert.False(mainVm.IsMainInterfaceVisible); // Исходный bool стейт из кода
            Assert.False(mainVm.IsAdminMenuVisible);     // Исходный bool стейт из кода
        }

    }




}
