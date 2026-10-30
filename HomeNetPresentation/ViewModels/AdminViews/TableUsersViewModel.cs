using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Interfaces.Sinc; // Подключаем интерфейс ОРМ-событий
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Builders;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class TableUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserRepository _userRepo;
        private readonly object _collectionLock = new();

        [ObservableProperty] private ObservableCollection<UserEntity> _users = [];

        public TableUsersViewModel(IEventBus eventBus,IUserRepository userRepo, AdminNavigationManager navigation)
            : base(eventBus, navigation)
        {
           _userRepo = userRepo;
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 🔥 ПЕРЕВОД НА ЦЕНТРАЛЬНЫЕ СОБЫТИЯ СЛОЯ ДАННЫХ (IDeleteUserVm / IUsersTableVm уходят в прошлое)
            _eventBus.Subscribe<ISiberNetSyncCoordinator.UserDeleted>(OnUserDeletedFromDb);
            _eventBus.Subscribe<ISiberNetSyncCoordinator.UserInserted>(OnUserAdded);
            _eventBus.Subscribe<ISiberNetSyncCoordinator.UserUpdated>(OnUserUpdated);

            _eventBus.Subscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
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
                var list = await _userRepo.GetAllAsync();

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

        #region 🎧 СИНХРОННЫЕ МЕТОДЫ ПОДПИСОК НА ЕДИНЫЙ ИСТОЧНИК ПРАВДЫ 🧼

        // Перешли на ISiberNetSyncCoordinator.UserUpdated
        private void OnUserUpdated(ISiberNetSyncCoordinator.UserUpdated msg)
        {
            if (msg?.User == null) return;

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

        // Перешли на ISiberNetSyncCoordinator.UserDeleted
        private void OnUserDeletedFromDb(ISiberNetSyncCoordinator.UserDeleted msg)
        {
            lock (_collectionLock)
            {
                var uiUser = Users.FirstOrDefault(u => u.Id == msg.UserId);
                if (uiUser != null)
                {
                    Users.Remove(uiUser);
                    BroadcastRefreshed();
                }
            }
        }

        // Перешли на ISiberNetSyncCoordinator.UserInserted
        private void OnUserAdded(ISiberNetSyncCoordinator.UserInserted msg)
        {
            if (msg.User == null) return;

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
            lock (_collectionLock)
            {
                _eventBus.Publish(this, new IUsersTableVm.Refreshed([.. Users]));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.UserDeleted>(OnUserDeletedFromDb);
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.UserInserted>(OnUserAdded);
            _eventBus.Unsubscribe<ISiberNetSyncCoordinator.UserUpdated>(OnUserUpdated);
            _eventBus.Unsubscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }
    }
}
