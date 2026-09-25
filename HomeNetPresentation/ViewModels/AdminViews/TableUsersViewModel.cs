using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    // 🔥 Реализуем IDisposable для безопасного вырезания подписок из памяти
    public partial class TableUsersViewModel : FormViewModelBase, IDisposable
    {
        private readonly IUserService _userService;

        [ObservableProperty] private ObservableCollection<UserEntity> _users = new();

        public TableUsersViewModel(IEventBus eventBus, IUserService userService, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            InitializeBusSubscriptions();
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            EventBus.Publish(this, new IStatusBarViewModel.TextChanged("Синхронизация с базой данных HomeNet..."));

            try
            {
                var list = await _userService.GetAllAsync();
                await Task.Delay(1000); // Кинематографичная пауза 🎬

                Users = new ObservableCollection<UserEntity>(list ?? Enumerable.Empty<UserEntity>());

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
            // 🔥 ЧИСТОТА: Передаем только ссылки на именованные методы класса вместо старой каши из лямбд!
            EventBus.Subscribe<IAdminMenuViewModel.UserTableRequested>(OnUserTableRequested);
            EventBus.Subscribe<IDeleteUserViewModel.Deleted>(OnUserDeleted);
            EventBus.Subscribe<IUsersTableViewModel.Added>(OnUserAdded);
            EventBus.Subscribe<IUsersTableViewModel.RefreshRequest>(OnRefreshRequest);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private async void OnUserTableRequested(IAdminMenuViewModel.UserTableRequested msg)
        {
            await InitializeDataAsync();
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
                    await Task.Delay(1000);
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

        private void OnRefreshRequest(IUsersTableViewModel.RefreshRequest msg)
        {
            EventBus.Publish(this, new IUsersTableViewModel.Refreshed(Users.ToList()));
        }

        #endregion

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        /// <summary>
        /// Вызывается при закрытии или уничтожении компонента таблицы.
        /// Полностью зачищает ссылки на методы, уберегая от утечек в долгоживущей шине событий.
        /// </summary>
        public void Dispose()
        {
            EventBus.Unsubscribe<IAdminMenuViewModel.UserTableRequested>(OnUserTableRequested);
            EventBus.Unsubscribe<IDeleteUserViewModel.Deleted>(OnUserDeleted);
            EventBus.Unsubscribe<IUsersTableViewModel.Added>(OnUserAdded);
            EventBus.Unsubscribe<IUsersTableViewModel.RefreshRequest>(OnRefreshRequest);
        }

        #endregion
    }
}