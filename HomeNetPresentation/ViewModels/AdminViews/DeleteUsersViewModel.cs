using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models;
using HomeNetPresentation.Services;
using System.Collections.ObjectModel;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Тонкая, автономная форма удаления пользователей SiberNet.
    /// Работает полностью на подсосе данных от центральной таблицы пользователей.
    /// </summary>
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

        // Перешли с ID на Email для ввода в техтбоксе
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        private string _targetUserEmail = string.Empty;

        public ObservableCollection<string> DeletedUsersHistory { get; } = new();

        public DeleteUsersViewModel(IDeleteService srv, IEventBus bus, ILogger log, AdminNavigationManager nav)
            : base(bus, nav)
        {
            _srv = srv ?? throw new ArgumentNullException(nameof(srv));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            SubmitButtonText = "Удалить";

            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeleted);

            ResetForm();
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Реакция на шину) 🧼

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

        private void OnUserDeleted(IDeleteUserVm.Deleted msg)
        {
            var userToRemove = FoundUsers.FirstOrDefault(u => u.Id == msg.Id);
            if (userToRemove != null)
            {
                FoundUsers.Remove(userToRemove);
            }
        }

        #endregion

        #region 🚀 КОМАНДЫ UI 🧼

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task SearchAsync()
        {
            StatusMessage = "Поиск...";
            // Сервис теперь дёргаем по Email (убедись, что метод в _srv принимает строку-email)
            var verdict = await _srv.SearchUserAsync(TargetUserEmail.Trim());
            StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

            if (verdict.IsValid && verdict.FoundUser != null)
            {
                // Чтобы не дублировать, если такой юзер уже есть в коллекции подсоса
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

                // Передаем TargetUserEmail вместо старого ID
                var verdict = await _srv.DeleteUserAsync(TargetUserEmail.Trim(), SelectedUser);
                UpdateValidation(verdict.Results);
                StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

                if (verdict.IsValid)
                {
                   

                    if (verdict.ParsedId.HasValue)
                    {
                        _eventBus.Publish(this, new IDeleteUserVm.Deleted(verdict.ParsedId.Value));
                    }
                    else
                    {
                        _log.LogError("Удаление не выполнено. Шина событий пропущена.");
                    }

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
        private async Task CancelAsync()
        {
            ResetForm();
            _eventBus.Publish(this, new IAdminVm.OpenDelete());
        }

        #endregion

        #region 🛠 ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ 🧼

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

        #endregion

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed);
            _eventBus.Unsubscribe<IDeleteUserVm.Deleted>(OnUserDeleted);
        }
    }
}
