using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Административная таблица пользователей SiberNet.
    /// Благодаря сквозному конвейеру бутстраппера вычищена от локов и потоковых костылей! 🧼
    /// </summary>
    public partial class TableUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IUserRepo _userRepo;

        [ObservableProperty] private ObservableCollection<UserEntity> _users = [];

        public TableUsersViewModel(IEventBus eventBus, IUserRepo userRepo, AdminNavigationManager navigation)
            : base(eventBus, navigation)
        {
            _userRepo = userRepo;
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 🔥 ВСЕ ЭТИ СОБЫТИЯ КОНВЕЙЕР БУТСТРАППЕРА ГАРАНТИРОВАННО ВЫРАВНЯЛ В UI-ПОТОК!
            _eventBus.Subscribe<IUserRepo.IDeleted>(OnUserDeletedFromDb);
            _eventBus.Subscribe<IUserRepo.IInserted>(OnUserAdded);
            _eventBus.Subscribe<IUserRepo.IUpdated>(OnUserUpdated);

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
                var list = await _userRepo.GetAllAsync<UserEntity>();

                // 🧼 Чистота: Запрос шел через await, возврат контекста гарантирован .NET
                Users = new ObservableCollection<UserEntity>(list ?? Enumerable.Empty<UserEntity>());

                BroadcastRefreshed();
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка старта таблицы: {ex.Message}"));
            }
        }

       

        private void OnUserUpdated(IUserRepo.IUpdated msg)
        {
            if (msg?.User == null) return;

            // Никаких локов — мы на скоростной трассе UI
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

        private void OnUserDeletedFromDb(IUserRepo.IDeleted msg)
        {
            var uiUser = Users.FirstOrDefault(u => u.Id == msg.UserId);
            if (uiUser != null)
            {
                Users.Remove(uiUser);
                BroadcastRefreshed();
            }
        }

        private void OnUserAdded(IUserRepo.IInserted msg)
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

      

        private void BroadcastRefreshed()
        {
            _eventBus.Publish(this, new IUsersTableVm.Refreshed([.. Users]));
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IUserRepo.IDeleted>(OnUserDeletedFromDb);
            _eventBus.Unsubscribe<IUserRepo.IInserted>(OnUserAdded);
            _eventBus.Unsubscribe<IUserRepo.IUpdated>(OnUserUpdated);
            _eventBus.Unsubscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
        }
    }
}
