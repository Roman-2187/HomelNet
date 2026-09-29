using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Центральный UI-источник правды для пользователей SiberNet.
    /// Хранит данные в словаре для защиты от дубликатов, но транслирует в ObservableCollection для UI.
    /// Загружается сразу со старта приложения.
    /// </summary>
    public partial class TableUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserService _userService;

        // 🔥 ДАЛЬНОВИДНОСТЬ: Внутреннее быстрое хранилище без дубликатов
        private readonly Dictionary<int, UserEntity> _usersMap = new();
        private bool _isLoaded = false;

        // Чистая коллекция для XAML привязок
        [ObservableProperty] private ObservableCollection<UserEntity> _users = new();

        public TableUsersViewModel(IEventBus eventBus, IUserService userService, AdminNavigationManager navigation)
            : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            InitializeBusSubscriptions();

            // 🔥 СО СТАРТА: Сразу греем кэш в фоновом потоке, не дожидаясь кликов по вкладкам!
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var list = await _userService.GetAllAsync();

                lock (_usersMap)
                {
                    _usersMap.Clear();
                    foreach (var user in list ?? Enumerable.Empty<UserEntity>())
                    {
                        _usersMap[user.Id] = user; // Защита от дублей на уровне ключей
                    }

                    // Синхронизируем UI коллекцию
                    Users = new ObservableCollection<UserEntity>(_usersMap.Values);
                }

                _isLoaded = true;

                // Сразу раздаем готовый список всем, кто сидит на подсосе
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

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК 🧼

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            // Если открыли вкладку таблицы — просто напоминаем актуальный список
            if (msg?.ActiveTab == AdminSubTab.UserTable && _isLoaded)
            {
                BroadcastRefreshed();
            }
        }

        private void OnUserDeletedFromDb(IDeleteUserVm.Deleted msg)
        {
            lock (_usersMap)
            {
                // Мгновенное удаление из словаря по ключу за O(1)!
                if (_usersMap.Remove(msg.Id))
                {
                    // Синхронизируем UI экран
                    var uiUser = Users.FirstOrDefault(u => u.Id == msg.Id);
                    if (uiUser != null) Users.Remove(uiUser);

                    // Сразу пинаем форму удаления и контакты, чтобы они убрали его у себя
                    BroadcastRefreshed();
                }
            }
        }

        private void OnUserAdded(IUsersTableVm.Added msg)
        {
            if (msg.User == null) return;

            lock (_usersMap)
            {
                // Если такой юзер уже прилетел по ошибке — словарь просто обновит его, а не продублирует!
                bool isNew = !_usersMap.ContainsKey(msg.User.Id);
                _usersMap[msg.User.Id] = msg.User;

                if (isNew)
                {
                    Users.Add(msg.User);
                }
                else
                {
                    // Если обновился — пересобираем UI коллекцию
                    Users = new ObservableCollection<UserEntity>(_usersMap.Values);
                }
            }

            BroadcastRefreshed();
        }

        private async void OnRefreshRequest(IUsersTableVm.RefreshRequest msg)
        {
            // 🔥 ЖЕЛЕЗОБЕТОННО: Если форма удаления просит данные, а таблица уже всё скачала
            if (_isLoaded)
            {
                BroadcastRefreshed(); // Просто выплевываем кэш из словаря
            }
            else
            {
                await InitializeDataAsync(); // Иначе экстренно докачиваем
            }
        }

        #endregion

        private void BroadcastRefreshed()
        {
            lock (_usersMap)
            {
                _eventBus.Publish(this, new IUsersTableVm.Refreshed(_usersMap.Values.ToList()));
            }
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
