namespace HomeNet.Tests
{
    //public class UsersTableViewModelTests
    //{
    //    [Fact] // 🔥 Наш радар для проверки загрузки пользователей из БД!
    //    public async Task UsersTableViewModel_ShouldLoadUsersFromDatabaseSuccessfully()
    //    {
    //        // 1. АРРАНЖ: Создаем фейковую базу данных (Mock/Заглушку) 🧼
    //        var fakeUsers = new List<UserEntity>
    //        {
    //            new() { Id = 1, FirstName = "Роман", LastName = "Архитектор", Email = "roman@sibernet.ru" },
    //            new() { Id = 2, FirstName = "UFO", LastName = "Дебаггер", Email = "ufo@sibernet.ru" }
    //        };

    //        // Подсовываем фейковый сервис, который честно вернет наш список пользователей
    //        IUserService testUserService = new FakeUserServiceForTest(fakeUsers);

    //        // Берем наш чистый проверенный автобус сообщений
    //        IEventBus testEventBus = new EventBus();

    //        // 2. АКТ: Создаем ВьюМодель! Она тут же запустит InitializeDataAsync() 🚀
    //        var viewModel = new TableUsersViewModel(testEventBus, testUserService);

    //        // 📢 ВАЖНО: Так как в коде вьюмодели зашита кинематографичная пауза Task.Delay(1000),
    //        // наш тест обязан подождать чуть больше (например, 1200 мс), чтобы асинхронный поток успел заполнить коллекцию!
    //        await Task.Delay(1200);

    //        // 3. АССЕРТ: Проверяем результаты насаждения данных 🎯🛸
    //        Assert.NotNull(viewModel.Users);
    //        Assert.Equal(2, viewModel.Users.Count); // Проверяем, что прилетело ровно 2 юзера!
    //        Assert.Equal("Роман", viewModel.Users[0].FirstName); // Первый в списке
    //        Assert.Equal("UFO", viewModel.Users[1].FirstName);   // Второй в списке
    //    }


    //    [Fact] // 🎯 Наш радар для проверки реактивного удаления пользователя!
    //    public async Task UsersTableViewModel_ShouldRemoveUserFromCollection_WhenUserDeletedMessageReceived()
    //    {
    //        // 1. АРРАНЖ: Готовим тестовых ребят на старте 🧼
    //        var initialUsers = new List<UserEntity>
    //{
    //    new() { Id = 777, FirstName = "Роман", LastName = "Архитектор", Email = "roman@sibernet.ru" },
    //    new() { Id = 999, FirstName = "Диверсант", LastName = "Багованный", Email = "bag@sibernet.ru" }
    //};

    //        IUserService fakeService = new FakeUserServiceForTest(initialUsers);
    //        IEventBus testEventBus = new EventBus();

    //        // Создаем ВьюМодель. Она загрузит двух юзеров и выдержит паузу в 1000мс
    //        var viewModel = new TableUsersViewModel(testEventBus, fakeService);

    //        // Ждем, пока InitializeDataAsync() отработает кинематографичную паузу и наполнит таблицу
    //        await Task.Delay(1200);
    //        Assert.Equal(2, viewModel.Users.Count); // Проверяем, что изначально оба на месте!

    //        // 2. АКТ: Стреляем из автобуса сигналом удаления "Диверсанта" (Id = "999")! 🚀🧨
    //        testEventBus.Publish(this, new UserDeletedMessage(999));

    //        // 📢 Так как внутри подписки на удаление зашит Task.Run с задержкой в 1000мс для строки состояния,
    //        // давай подождем 1100мс, чтобы фоновый поток успел полностью завершить свою магию!
    //        await Task.Delay(1100);

    //        // 3. АССЕРТ: Проверяем, что диверсант уничтожен, а Роман остался один в безопасности! 🎯🏆
    //        Assert.Single(viewModel.Users); // В таблице должен остаться ровно ОДИН пользователь!
    //        Assert.Equal(777, viewModel.Users[0].Id); // И это должен быть Роман!
    //        Assert.Equal("Роман", viewModel.Users[0].FirstName);
    //    }



    //    private class FakeUserServiceForTest : IUserService
    //    {
    //        private readonly List<UserEntity> _users; // 🔥 Переводим внутреннее поле в List!

    //        // В конструкторе принудительно приводим прилетевшую коллекцию к List
    //        public FakeUserServiceForTest(IEnumerable<UserEntity> users) => _users = users.ToList();

    //        // 1. Наш главный метод для теста таблицы — теперь возвращает строго Task<List<UserEntity>>! 🎯🧼
    //        public Task<List<UserEntity>> GetAllUsersAsync() => Task.FromResult(_users);

    //        // 2. Закрываем абсолютно все остальные методы интерфейса Ядра дефолтными пустышками! 🛸✨
    //        public Task<bool> CheckEmailExistsAsync(string email) => Task.FromResult(false);
    //        public Task AddUserAsync(UserEntity user) => Task.CompletedTask;
    //        public Task DeleteUserAsync(int id, string reason) => Task.CompletedTask;
    //        public Task<UserEntity?> FindUserByEmailAsync(string email) => Task.FromResult<UserEntity?>(null);
    //        public Task<IEnumerable<UserEntity>> GetActiveUsersAsync() => Task.FromResult(System.Linq.Enumerable.Empty<UserEntity>());

    //        // 3. Вот эти два потерянных диверсанта, которых требовал компилятор! 🕵️‍♂️🚨
    //        public Task<UserEntity?> GetUserByEmailAsync(string email) => Task.FromResult<UserEntity?>(null);
    //        public Task<UserEntity?> GetUserByIdAsync(int id) => Task.FromResult<UserEntity?>(null);
    //    }



    //}
}
