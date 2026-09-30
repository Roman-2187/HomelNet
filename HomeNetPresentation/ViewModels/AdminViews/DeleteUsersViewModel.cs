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

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        private string _targetUserId = string.Empty;

        public ObservableCollection<string> DeletedUsersHistory { get; } = new();

        public DeleteUsersViewModel(IDeleteService srv, IEventBus bus, ILogger log, AdminNavigationManager nav)
            : base(bus, nav)
        {
            _srv = srv ?? throw new ArgumentNullException(nameof(srv));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            SubmitButtonText = "Удалить";

            _eventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersRefreshed); // 🔥 ПОДСОС: Ловим общий список
            _eventBus.Subscribe<IDeleteUserVm.Deleted>(OnUserDeleted);

            ResetForm();
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Реакция на шину) 🧼

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged msg)
        {
            if (msg?.ActiveTab == AdminSubTab.DeleteUsers)
            {
                ResetForm();
                // 🔥 ПИНГ: Просим таблицу выплюнуть нам актуальный кэш
                _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
            }
        }

        private void OnUsersRefreshed(IUsersTableVm.Refreshed msg)
        {
            if (msg == null) return;
            // 🔥 ПОДСОС В ДЕЙСТВИИ: Просто забираем то, что прислала таблица
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
            var verdict = await _srv.SearchUserAsync(TargetUserId);
            StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

            if (verdict.IsValid && verdict.FoundUser != null)
            {
                FoundUsers.Add(verdict.FoundUser);
                SelectedUser = verdict.FoundUser;
            }
        }

        [RelayCommand]
        private async Task ExecuteDeleteAsync()
        {
            try
            {
                StatusMessage = "Удаление...";

                var verdict = await _srv.DeleteUserAsync(TargetUserId, SelectedUser);
                UpdateValidation(verdict.Results);
                StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

                if (verdict.IsValid)
                {
                    _log.LogInfo($"[DeleteVM] Юзер {verdict.ParsedId} стёрт.");

                    _eventBus.Publish(this, new IDeleteUserVm.Deleted(verdict.ParsedId ?? -1));

                    if (!string.IsNullOrEmpty(verdict.HistoryMessage))
                    {
                        DeletedUsersHistory.Add(verdict.HistoryMessage);
                    }

                    await Task.Delay(500);
                    ResetForm();
                    _eventBus.Publish(this, new IUsersTableVm.RefreshRequest()); // Обновляем подсос
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
            TargetUserId = string.Empty;
            SelectedUser = null;
            OnPropertyChanged(nameof(TargetUserId));
            OnPropertyChanged(nameof(SelectedUser));
            StatusMessage = "Введите ID или выберите пользователя";
        }

        private bool CanSearch()
        {
            if (FoundUsers.Count == 0 && string.IsNullOrWhiteSpace(TargetUserId)) return false;
            if (SelectedUser == null) return true;
            return SelectedUser.Id.ToString() != TargetUserId.Trim();
        }

        partial void OnSelectedUserChanged(UserEntity? value)
        {
            if (value != null && TargetUserId != value.Id.ToString())
            {
                _targetUserId = value.Id.ToString();
                OnPropertyChanged(nameof(TargetUserId));
            }

            StatusMessage = value != null
                ? $"Выбран: ID {value.Id} — {value.FirstName} {value.LastName}"
                : $"Всего в базе: {FoundUsers.Count}. Выберите юзера.";
        }

        partial void OnTargetUserIdChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _selectedUser = null;
                OnPropertyChanged(nameof(SelectedUser));
                return;
            }

            var match = FoundUsers.FirstOrDefault(u => u.Id.ToString() == value.Trim());
            _selectedUser = match;
            OnPropertyChanged(nameof(SelectedUser));

            if (match == null)
                StatusMessage = $"Введён сторонний ID: {value}. Нажмите 'Поиск'.";
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
