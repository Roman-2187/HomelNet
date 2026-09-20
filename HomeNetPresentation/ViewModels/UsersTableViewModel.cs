using HomeNetCore.Interfaces;             
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; 
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    public partial class UsersTableViewModel : FormViewModelBase
    {
        private readonly IUserService _userService;

        // ObservableCollection Тулкит сам обернёт в свойство, если нужно, но мы оставляем её открытой для биндинга
        public ObservableCollection<UserEntity> Users { get; private set; } = new();

        // Конструктор принимает чистый IEventBus из Ядра и прокидывает в базу через base(eventBus)
        public UsersTableViewModel(IEventBus eventBus, IUserService userService, NavigationStateManager navigation) : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            InitializeBusSubscriptions();

            // Запускаем асинхронную подгрузку без жёстких Task.Run внутри конструктора
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            // 🔥 ПОПРАВИЛИ: Короткий рекорд строки состояния
            EventBus.Publish(this, new IStatusBarViewModel.TextChanged("Синхронизация с базой данных HomeNet..."));

            try
            {
                // 1. Честно читаем базу данных напрямую через асинхронный метод сервиса
                var list = await _userService.GetAllAsync();

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

                // 🔥 ПОПРАВИЛИ: Кормим статус-бар финальными циферками через короткие рекорды
                EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
                EventBus.Publish(this, new IStatusBarViewModel.TextChanged("База данных успешно синхронизирована."));
            }
            catch (Exception ex)
            {
                EventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка синхронизации данных: {ex.Message}"));
            }
        }

        private void InitializeBusSubscriptions()
        {
            // 1. 🔥 ПОПРАВИЛИ: Ловим короткое сообщение об удалении пользователя
            EventBus.Subscribe<IDeleteUserViewModel.Deleted>(msg =>
            {
                var userToRemove = Users.FirstOrDefault(u => u.Id == msg.UserId);
                if (userToRemove != null)
                {
                    string deletedName = $"{userToRemove.FirstName} {userToRemove.LastName}";
                    Users.Remove(userToRemove);

                    // 🔥 ПОПРАВИЛИ: Стреляем точной укороченной коллекцией сразу после удаления!
                    EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));

                    // Асинхронный статус без блокировки потоков
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        EventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Пользователь {deletedName} успешно удалён"));
                    });
                }
            });

            // 2. 🔥 ПОПРАВИЛИ: Ловим короткое добавление нового пользователя
            EventBus.Subscribe<IUsersTableViewModel.Added>(msg =>
            {
                if (msg.User == null) return;

                Users.Add(msg.User);
                EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
            });

            // 3. 🔥 ПОПРАВИЛИ: ОТВЕТ НА ЗАПРОС: Строка состояния попросила обновить экран (короткий рекорд)
            EventBus.Subscribe<IUsersTableViewModel.RefreshRequest>(msg =>
            {
                EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
            });
        }
    }
}
