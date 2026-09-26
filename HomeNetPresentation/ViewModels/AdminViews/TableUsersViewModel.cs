using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class TableUsersViewModel : FormViewModelBase, IDisposable
    {
        private readonly IUserService _userService;
        private bool _isLoaded = false; // 🔥 Флаг-предохранитель, чтобы не спамить базу при каждом повторном клике

        [ObservableProperty] private ObservableCollection<UserEntity> _users = new();

        public TableUsersViewModel(IEventBus eventBus, IUserService userService, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            InitializeBusSubscriptions();

            // 🧼 ТУТ ТЕПЕРЬ СТЕРЕЛЬНАЯ ЧИСТОТА: Никаких вызовов InitializeDataAsync() на старте!
        }

        private async Task InitializeDataAsync()
        {     

            try
            {
                var list = await _userService.GetAllAsync();
               

                Users = new ObservableCollection<UserEntity>(list ?? Enumerable.Empty<UserEntity>());
                _isLoaded = true; // Фиксируем, что данные успешно подтянулись

                EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
                EventBus.Publish(this, new IStatusBarViewModel.TextChanged($"База данных успешно синхронизирована загружено {Users.Count} пользователей"));
            }
            catch (Exception ex)
            {
                EventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка синхронизации данных: {ex.Message}"));
            }
        }

        private void InitializeBusSubscriptions()
        {
            EventBus.Subscribe<IAdminMenuViewModel.UserTableRequested>(OnUserTableRequested);
            EventBus.Subscribe<IDeleteUserViewModel.Deleted>(OnUserDeleted);
            EventBus.Subscribe<IUsersTableViewModel.Added>(OnUserAdded);
            EventBus.Subscribe<IUsersTableViewModel.RefreshRequest>(OnRefreshRequest);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК 🧼

        private async void OnUserTableRequested(IAdminMenuViewModel.UserTableRequested msg)
        {
            // 🔥 ЛЕНИВАЯ ЗАГРУЗКА: Идём в базу ТОЛЬКО если таблица открыта ВПЕРВЫЕ
            if (!_isLoaded)
            {
                await InitializeDataAsync();
            }
            else
            {
                // Если данные уже есть в памяти синглтона — просто пушим их в интерфейс мгновенно
                EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
            }
        }

        private void OnUserDeleted(IDeleteUserViewModel.Deleted msg)
        {
            var userToRemove = Users.FirstOrDefault(u => u.Id == msg.Id);
            if (userToRemove != null)
            {
                string deletedName = $"{userToRemove.FirstName} {userToRemove.LastName}";
                Users.Remove(userToRemove);

                EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));

                _ = Task.Run(async () =>
                {
                    
                    EventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Пользователь {deletedName} успешно удалён"));
                });
            }
        }

        private void OnUserAdded(IUsersTableViewModel.Added msg)
        {
            if (msg.User == null) return;
            Users.Add(msg.User);
            EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
        }

        private async void OnRefreshRequest(IUsersTableViewModel.RefreshRequest msg)
        {
            // При принудительном рефреше (кнопкой) — плевать на флаг, жестко перечитываем базу
            await InitializeDataAsync();
        }

        #endregion

        public void Dispose()
        {
            EventBus.Unsubscribe<IAdminMenuViewModel.UserTableRequested>(OnUserTableRequested);
            EventBus.Unsubscribe<IDeleteUserViewModel.Deleted>(OnUserDeleted);
            EventBus.Unsubscribe<IUsersTableViewModel.Added>(OnUserAdded);
            EventBus.Unsubscribe<IUsersTableViewModel.RefreshRequest>(OnRefreshRequest);
        }
    }
}
