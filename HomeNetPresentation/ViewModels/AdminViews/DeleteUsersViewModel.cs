using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Interfaces.Sinc; 
using HomeNetCore.Models.Entity;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;

namespace HomeNetPresentation.ViewModels
{
    public partial class DeleteUsersViewModel : FormViewModelBase<AdminNavigationManager>, IDisposable
    {
        private readonly IDeleteService _srv;
        private readonly ILogger _log;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        private ObservableCollection<UserEntity> _foundUsers = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        private UserEntity? _selectedUser;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        private string _targetUserEmail = string.Empty;

        public ObservableCollection<string> DeletedUsersHistory { get; } = [];

        public DeleteUsersViewModel(IDeleteService srv, IEventBus bus, ILogger log, AdminNavigationManager nav)
            : base(bus, nav)
        {
            _srv = srv ?? throw new ArgumentNullException(nameof(srv));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            SubmitButtonText = "Удалить";

            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);

            // 🔥 ПОДПИСКА НА ЕДИНЫЙ ИСТОЧНИК ПРАВДЫ ОТ ОРМ
            _eventBus.Subscribe<ISyncCoordinator.UserDeleted>(OnUserDeleted);

            ResetForm();
        }


        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg?.ActiveTab == AdminSubTab.DeleteUsers)
            {
                ResetForm();
                _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
            }
        }

        private void OnUsersRefreshed(IUsersTableVm.Refreshed msg)
        {
            if (msg == null) return;
            FoundUsers = new ObservableCollection<UserEntity>(msg.Users);
        }

        // 🔥 Метод теперь принимает системный тип события от репозитория
        private void OnUserDeleted(ISyncCoordinator.UserDeleted msg)
        {
            var userToRemove = FoundUsers.FirstOrDefault(u => u.Id == msg.UserId);
            if (userToRemove != null)
            {
                FoundUsers.Remove(userToRemove);
            }
        }

       

       

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task SearchAsync()
        {
            StatusMessage = "Поиск...";
            var verdict = await _srv.SearchUserAsync(TargetUserEmail.Trim());
            StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

            if (verdict.IsValid && verdict.FoundUser != null)
            {
                if (FoundUsers.All(u => u.Id != verdict.FoundUser.Id))
                {
                    FoundUsers.Add(verdict.FoundUser);
                }
                SelectedUser = verdict.FoundUser;
            }
        }

        [RelayCommand]
        private async Task ExecuteDeleteAsync()
        {
            try
            {
                StatusMessage = "Удаление...";

                var verdict = await _srv.DeleteUserAsync(TargetUserEmail.Trim(), SelectedUser);
                UpdateValidation(verdict.Results);
                StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

                if (verdict.IsValid)
                {

                    if (!string.IsNullOrEmpty(verdict.HistoryMessage))
                    {
                        DeletedUsersHistory.Add(verdict.HistoryMessage);
                    }

                    await Task.Delay(500);
                    ResetForm();
                    _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CancelAsync() =>ResetForm();
       
       
        private void ResetForm()
        {
            TargetUserEmail = string.Empty;
            SelectedUser = null;
            OnPropertyChanged(nameof(TargetUserEmail));
            OnPropertyChanged(nameof(SelectedUser));
            StatusMessage = "Введите Email или выберите пользователя";
        }

        private bool CanSearch()
        {
            if (FoundUsers.Count == 0 && string.IsNullOrWhiteSpace(TargetUserEmail)) return false;
            if (SelectedUser == null) return true;
            return !string.Equals(SelectedUser.Email?.Trim(), TargetUserEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        partial void OnSelectedUserChanged(UserEntity? value)
        {
            if (value != null && !string.Equals(TargetUserEmail, value.Email, StringComparison.OrdinalIgnoreCase))
            {
                _targetUserEmail = value.Email ?? string.Empty;
                OnPropertyChanged(nameof(TargetUserEmail));
            }

            StatusMessage = value != null
                ? $"Выбран: {value.Email} — {value.FirstName} {value.LastName}"
                : $"Всего в базе: {FoundUsers.Count}. Выберите юзера.";
        }

        partial void OnTargetUserEmailChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _selectedUser = null;
                OnPropertyChanged(nameof(SelectedUser));
                return;
            }

            var match = FoundUsers.FirstOrDefault(u => string.Equals(u.Email?.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase));
            _selectedUser = match;
            OnPropertyChanged(nameof(SelectedUser));

            if (match == null)
                StatusMessage = $"Введён сторонний Email: {value}. Нажмите 'Поиск'.";
        }

       

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);
            _eventBus.Unsubscribe<ISyncCoordinator.UserDeleted>(OnUserDeleted);
        }
    }
}
