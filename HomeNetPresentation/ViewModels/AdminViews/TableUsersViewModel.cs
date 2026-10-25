using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Builders;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class TableUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserService _userService;

        // 🔥 ВСТРОЕННЫЙ ОБЪЕКТ СИНХРОНИЗАЦИИ .NET: Гарантирует потокобезопасность коллекции
        private readonly object _collectionLock = new();

        [ObservableProperty] private ObservableCollection<UserEntity> _users = [];

        public TableUsersViewModel(IEventBus eventBus, IUserService userService, AdminNavigationManager navigation)
            : base(eventBus, navigation)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeletedFromDb);
            _eventBus.Subscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Subscribe<IUsersTableVm.Updated>(OnUserUpdated);
            _eventBus.Subscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);

            // Ждем, пока Хаб баз данных отмаринует и прогреет обе СУБД!
            _eventBus.Subscribe<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);
        }

        private async void OnInfrastructureReady(IDbContextContainer.InfrastructureReady msg)
        {
            await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var list = await _userService.GetAllAsync();

                // 🔥 ЗАЩИТА ИНИЦИАЛИЗАЦИИ: Блокируем гонки потоков при полной перезаписи коллекции
                lock (_collectionLock)
                {
                    Users = new ObservableCollection<UserEntity>(list ?? Enumerable.Empty<UserEntity>());
                }

                BroadcastRefreshed();
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка старта таблицы: {ex.Message}"));
            }
        }

        #region 🎧 МЕТОДЫ ПОДПИСОК С ЖЕСТКОЙ СИНХРОНИЗАЦИЕЙ ПОТОКОВ 🧼

        private void OnUserUpdated(IUsersTableVm.Updated msg)
        {
            if (msg?.User == null) return;

            // 🔥 БЕЗОПАСНЫЙ АПДЕЙТ: Ни один фоновый поток не вобьет клин при замене объекта по индексу
            lock (_collectionLock)
            {
                var existingUser = Users.FirstOrDefault(u => u.Id == msg.User.Id);
                if (existingUser != null)
                {
                    int index = Users.IndexOf(existingUser);
                    if (index >= 0)
                    {
                        Users[index] = msg.User;
                        BroadcastRefreshed();
                    }
                }
            }
        }

        private void OnUserDeletedFromDb(IDeleteUserVm.Deleted msg)
        {
            // 🔥 БЕЗОПАСНОЕ УДАЛЕНИЕ: Защита от выброса InvalidOperationException (коллекция была изменена)
            lock (_collectionLock)
            {
                var uiUser = Users.FirstOrDefault(u => u.Id == msg.Id);
                if (uiUser != null)
                {
                    Users.Remove(uiUser);
                    BroadcastRefreshed();
                }
            }
        }

        private void OnUserAdded(IUsersTableVm.Added msg)
        {
            if (msg.User == null) return;

            // 🔥 БЕЗОПАСНОЕ ДОБАВЛЕНИЕ: Потоки выстраиваются в очередь на запись
            lock (_collectionLock)
            {
                if (!Users.Any(u => u.Id == msg.User.Id))
                {
                    Users.Add(msg.User);
                    BroadcastRefreshed();
                }
            }
        }

        private async void OnRefreshRequest(IUsersTableVm.RefreshRequest msg)
        {
            await InitializeDataAsync();
        }

        #endregion

        private void BroadcastRefreshed()
        {
            // Блокируем чтение при клонировании коллекции для отправки в шину
            lock (_collectionLock)
            {
                _eventBus.Publish(this, new IUsersTableVm.Refreshed([.. Users]));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeletedFromDb);
            _eventBus.Unsubscribe<IUsersTableVm.Added>(OnUserAdded);
            _eventBus.Unsubscribe<IUsersTableVm.Updated>(OnUserUpdated);
            _eventBus.Unsubscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }
    }
}
