using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class TableUsersViewModel : FormViewModelBase
    {
        private readonly IUserService _userService;

        public ObservableCollection<UserEntity> Users { get; private set; } = new();

        public TableUsersViewModel(IEventBus eventBus, IUserService userService, NavigationStateManager navigation) : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            InitializeBusSubscriptions();

            // Запускаем первичную подгрузку один раз при старте
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            EventBus.Publish(this, new IStatusBarViewModel.TextChanged("Синхронизация с базой данных HomeNet..."));

            try
            {
                var list = await _userService.GetAllAsync();

                await Task.Delay(1000); // Кинематографичная пауза 🎬

                Users.Clear();
                if (list != null)
                {
                    foreach (var user in list)
                    {
                        Users.Add(user);
                    }
                }

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
            // 🎯 ПЕРЕНЕСЛИ СЮДА: Ловим пинок админа на обновление (строго одна подписка)
            EventBus.Subscribe<IAdminMenuViewModel.UserTableRequested>(async msg =>
            {
                await InitializeDataAsync();
            });

            // 1. Ловим удаление пользователя
            EventBus.Subscribe<IDeleteUserViewModel.Deleted>(msg =>
            {
                var userToRemove = Users.FirstOrDefault(u => u.Id == msg.UserId);
                if (userToRemove != null)
                {
                    string deletedName = $"{userToRemove.FirstName} {userToRemove.LastName}";
                    Users.Remove(userToRemove);

                    EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        EventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Пользователь {deletedName} успешно удалён"));
                    });
                }
            });

            // 2. Ловим добавление нового пользователя
            EventBus.Subscribe<IUsersTableViewModel.Added>(msg =>
            {
                if (msg.User == null) return;

                Users.Add(msg.User);
                EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
            });

            // 3. Ответ на запрос синхронизации состояния экрана
            EventBus.Subscribe<IUsersTableViewModel.RefreshRequest>(msg =>
            {
                EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
            });
        }
    }
}
