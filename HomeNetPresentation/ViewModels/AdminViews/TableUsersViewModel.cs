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
    /// <summary>
    /// Облегченный UI-источник правды для пользователей SiberNet.
    /// Никакой инфраструктуры, только чистый async/await и точечные обновления коллекции.
    /// </summary>
    public partial class TableUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserService _userService;
        private bool _isLoaded = false;

        // Коллекция инициализируется один раз и больше никогда не пересоздается через new!
        [ObservableProperty] private ObservableCollection<UserEntity> _users = new();

        public TableUsersViewModel(IEventBus eventBus, IUserService userService, AdminNavigationManager navigation)
            : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            InitializeBusSubscriptions();

            // 🔥 Пинаем фоновый старт. С контекстом async/await возврат произойдет в UI-поток сам.
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
               

                var list = await Task.Run(() => _userService.GetAllAsync());

                // Очищаем и точечно заполняем ОДНУ И ТУ ЖЕ коллекцию
                Users.Clear();
                foreach (var user in list ?? Enumerable.Empty<UserEntity>())
                {
                    Users.Add(user);
                }

                _isLoaded = true;
                BroadcastRefreshed();
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка старта таблицы: {ex.Message}"));
            }
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeletedFromDb);
            _eventBus.Subscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Subscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }

        #region 🎧 МЕТОДЫ ПОДПИСОК 🧼

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg?.ActiveTab == AdminSubTab.UserTable && _isLoaded)
            {
                BroadcastRefreshed();
            }
        }

        private void OnUserDeletedFromDb(IDeleteUserVm.Deleted msg)
        {
            // Точечное удаление
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

            // Точечное добавление, если такого еще нет
            if (!Users.Any(u => u.Id == msg.User.Id))
            {
                Users.Add(msg.User);
                BroadcastRefreshed();
            }
        }

        private async void OnRefreshRequest(IUsersTableVm.RefreshRequest msg)
        {
            if (_isLoaded)
            {
                BroadcastRefreshed();
            }
            else
            {
                await InitializeDataAsync();
            }
        }

        #endregion

        private void BroadcastRefreshed()
        {
            _eventBus.Publish(this, new IUsersTableVm.Refreshed(Users.ToList()));
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeletedFromDb);
            _eventBus.Unsubscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Unsubscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }
    }
}
