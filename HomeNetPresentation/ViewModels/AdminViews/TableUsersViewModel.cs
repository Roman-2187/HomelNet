using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Enums.Navigation; // Подтягиваем наши энумы вкладок админки
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Живая таблица пользователей SiberNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера навигации админки.
    /// </summary>
    public partial class TableUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserService _userService;
        private bool _isLoaded = false; // 🔥 Флаг-предохранитель, чтобы не спамить базу при каждом повторном клике

        [ObservableProperty] private ObservableCollection<UserEntity> _users = new();

        public TableUsersViewModel(IEventBus eventBus, IUserService userService, AdminNavigationManager navigation)
            : base(eventBus, navigation) // Передали строго типизированный навигатор в generic-базу
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            InitializeBusSubscriptions();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var list = await _userService.GetAllAsync();

                Users = new ObservableCollection<UserEntity>(list ?? Enumerable.Empty<UserEntity>());
                _isLoaded = true; // Фиксируем, что данные успешно подтянулись

                _eventBus.Publish(this, new IUsersTableVm.Refreshed(Users.ToList()));
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"База данных успешно синхронизирована загружено {Users.Count} пользователей"));
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка синхронизации данных: {ex.Message}"));
            }
        }

        private void InitializeBusSubscriptions()
        {
            // 🔥 ЧИСТОТА ДЛЯ ИНСПЕКТОРА: Никаких лямбд, только именованные обработчики! 🧼
            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeleted);
            _eventBus.Subscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Subscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private async void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg == null) return;

            // 🔥 ЕСЛИ НАВИГАТОР ПОДТВЕРДИЛ: Открыта вкладка таблицы пользователей!
            if (msg.ActiveTab == AdminSubTab.UserTable)
            {
                // 🔥 ЛЕНИВАЯ ЗАГРУЗКА: Идём в базу ТОЛЬКО если таблица открыта ВПЕРВЫЕ
                if (!_isLoaded)
                {
                    await InitializeDataAsync();
                }
                else
                {
                    // Если данные уже есть в памяти синглтона — просто пушим их в интерфейс мгновенно
                    _eventBus.Publish(this, new IUsersTableVm.Refreshed(Users.ToList()));
                }
            }
        }

        private void OnUserDeleted(IDeleteUserVm.Deleted msg)
        {
            var userToRemove = Users.FirstOrDefault(u => u.Id == msg.Id);
            if (userToRemove != null)
            {
                string deletedName = $"{userToRemove.FirstName} {userToRemove.LastName}";
                Users.Remove(userToRemove);

                _eventBus.Publish(this, new IUsersTableVm.Refreshed(Users.ToList()));

                _ = Task.Run(() =>
                {
                    _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Пользователь {deletedName} успешно удалён"));
                });
            }
        }

        private void OnUserAdded(IUsersTableVm.Added msg)
        {
            if (msg.User == null) return;
            Users.Add(msg.User);
            _eventBus.Publish(this, new IUsersTableVm.Refreshed(Users.ToList()));
        }

        private async void OnRefreshRequest(IUsersTableVm.RefreshRequest msg)
        {
            // При принудительном рефреше (кнопкой) — плевать на флаг, жестко перечитываем базу
            await InitializeDataAsync();
        }

        #endregion

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose(); // Чистим базовые ресурсы дженерик-базы
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeleted);
            _eventBus.Unsubscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Unsubscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }

        #endregion
    }
}
