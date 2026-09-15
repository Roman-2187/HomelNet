using HomeNetCore.Interfaces;
using HomeNetCore.Messaging;              // Чистые сигналы-рекорды из Ядра
using HomeNetCore.Models;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class UsersTableViewModel : FormViewModelBase
    {
        private readonly IUserService _userService;

        // ObservableCollection Тулкит сам обернёт в свойство, если нужно, но мы оставляем её открытой для биндинга
        public ObservableCollection<UserEntity> Users { get; private set; } = new();

        // Конструктор принимает чистый IEventBus из Ядра и прокидывает в базу через base(eventBus)
        public UsersTableViewModel(IEventBus eventBus, IUserService userService) : base(eventBus)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            InitializeBusSubscriptions();

            // Запускаем асинхронную подгрузку без жёстких Task.Run внутри конструктора
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            // Публикуем статус через базовое свойство EventBus с БОЛЬШОЙ буквы! 🧼🛸
            EventBus.Publish(this, new StatusTextChangedMessage("Синхронизация с базой данных HomeNet..."));

            try
            {
                // 1. Честно читаем базу данных напрямую через асинхронный метод сервиса
                var list = await _userService.GetAllUsersAsync();

                await Task.Delay(1000); // Наша кинематографичная пауза 🎬

                // 2. Очищаем и заполняем. SynchronizationContext шины сам вернет этот поток в UI (WPF/Avalonia)
                Users.Clear();
                if (list != null)
                {
                    foreach (var user in list)
                    {
                        Users.Add(user);
                    }
                }

                // Кормим статус-бар финальными циферками
                EventBus.Publish(this, new UsersListRefreshedMessage(Users.ToList()));
                EventBus.Publish(this, new StatusTextChangedMessage("База данных успешно синхронизирована."));
            }
            catch (Exception ex)
            {
                EventBus.Publish(this, new StatusTextChangedMessage($"Ошибка синхронизации данных: {ex.Message}"));
            }
        }

        private void InitializeBusSubscriptions()
        {
            // 1. Ловим сообщение об удалении пользователя
            EventBus.Subscribe<UserDeletedMessage>(msg =>
            {
                var userToRemove = Users.FirstOrDefault(u => u.Id == msg.UserId);
                if (userToRemove != null)
                {
                    string deletedName = $"{userToRemove.FirstName} {userToRemove.LastName}";
                    Users.Remove(userToRemove);

                    // Стреляем точной коллекцией сразу после удаления!
                    EventBus.Publish(this, new UsersListRefreshedMessage(Users.ToList()));

                    // Асинхронный статус без блокировки потоков
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        EventBus.Publish(this, new StatusTextChangedMessage($"Пользователь {deletedName} успешно удалён"));
                    });
                }
            });

            // 2. Ловим добавление нового пользователя (исправили на UserAddedMessage для стыковки!) 🧼
            EventBus.Subscribe<UserAddedMessage>(msg =>
            {
                if (msg.User == null) return;

                Users.Add(msg.User);
                EventBus.Publish(this, new UsersListRefreshedMessage(Users.ToList()));
            });

            // 3. ОТВЕТ НА ЗАПРОС: Строка состояния попросила обновить экран
            EventBus.Subscribe<RequestStatusRefreshMessage>(msg =>
            {
                EventBus.Publish(this, new UsersListRefreshedMessage(Users.ToList()));
            });
        }
    }
}
