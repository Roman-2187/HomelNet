using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class TableUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserService _userService;

        [ObservableProperty] private ObservableCollection<UserEntity> _users = new();

        public TableUsersViewModel(IEventBus eventBus, IUserService userService, AdminNavigationManager navigation)
            : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            // 🔥 ВСЁ СНОВА ЗДЕСЬ: Чистые, изолированные подписки на изменения данных
            InitializeBusSubscriptions();

            // 🔥 ЗАПУСК ИЗ КОНСТРУКТОРА: Метод выстрелит сразу в момент рождения класса в памяти!
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var list = await _userService.GetAllAsync();

                // Прямая подмена ссылки для железного апдейта Avalonia
                Users = new ObservableCollection<UserEntity>(list ?? Enumerable.Empty<UserEntity>());

                BroadcastRefreshed();
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка старта таблицы: {ex.Message}"));
            }
        }

        private void InitializeBusSubscriptions()
        {
            
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeletedFromDb);
            _eventBus.Subscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Subscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }

        #region 🎧 МЕТОДЫ ПОДПИСОК 🧼

        private void OnUserDeletedFromDb(IDeleteUserVm.Deleted msg)
        {
            var uiUser = Users.FirstOrDefault(u => u.Id == msg.Id);
            if (uiUser != null)
            {
                Users.Remove(uiUser);
                BroadcastRefreshed();
            }
        }

        private void OnUserAdded(IUsersTableVm.Added msg)
        {
            if (msg.User == null) return;
            if (!Users.Any(u => u.Id == msg.User.Id))
            {
                Users.Add(msg.User);
                BroadcastRefreshed();
            }
        }

        private async void OnRefreshRequest(IUsersTableVm.RefreshRequest msg)
        {
            await InitializeDataAsync();
        }

        #endregion

        private void BroadcastRefreshed()
        {
            _eventBus.Publish(this, new IUsersTableVm.Refreshed(Users.ToList()));
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeletedFromDb);
            _eventBus.Unsubscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Unsubscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }
    }
}
