using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Windows;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public partial class DeleteUsersViewModel : FormViewModelBase
    {
        #region Поля и свойства 🦾
        private readonly DeleteService _deleteService;

        ILogger  _logger;

        // ПОЛНОСТЬЮ убрали свойство MainUsersList! Окно больше не следит за всей коллекцией 🧼
        [ObservableProperty] private UserEntity? _selectedUser;
        [ObservableProperty] private string _targetUserId = string.Empty;
        [ObservableProperty] private bool _canDelete;
        #endregion

        #region Конструктор
        public DeleteUsersViewModel(DeleteService deleteService, EventBus eventBus,ILogger logger) : base(eventBus)
        {
            _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));


            ControlVisibility = Visibility.Collapsed;
            SubmitButtonText = "Удалить";
            StatusMessage = "Введите ID ";

            InitEventBus();

            _logger=logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Хитрые хуки (Нано-реакция на изменение полей) ⚙️
        partial void OnSelectedUserChanged(UserEntity? value)
        {
            if (value != null)
            {
                TargetUserId = value.DisplayInfo.ToString();
                CanDelete = true;
            }
        }

        partial void OnTargetUserIdChanged(string value)
        {
            SearchCommand.NotifyCanExecuteChanged();
            // Тернарное сжатие условий! 🛸
            CanDelete = int.TryParse(value, out int parsedId) && parsedId > 0 || (!string.IsNullOrWhiteSpace(value) && CanDelete);
        }

        partial void OnCanDeleteChanged(bool value) => DeleteCommand.NotifyCanExecuteChanged();
        #endregion

        #region Нано-команды (Чистая логика без мусора) 🧼

        private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(TargetUserId);

        [RelayCommand(CanExecute = nameof(CanExecuteSearch))]
        private async Task SearchAsync()
        {
            StatusMessage = "Поиск пользователя в базе данных...";
            CanDelete = false;
            var (isSuccess, message, _) = await _deleteService.SearchUserAsync(TargetUserId);
            StatusMessage = message;
            CanDelete = isSuccess;
        }

        private bool CanExecuteDelete() => CanDelete;

        [RelayCommand(CanExecute = nameof(CanExecuteDelete))]
        private async Task DeleteAsync()
        {
            // Тернарник находит нужный ID в одну строку! 👌
            int id = SelectedUser?.Id ?? (int.TryParse(TargetUserId, out int parsedId) ? parsedId : -1);
            if (id == -1) return;

            StatusMessage = $"Удаление пользователя с ID {id}...";
            var (isSuccess, message) = await _deleteService.DeleteUserAsync(id);
            StatusMessage = message;


            if (isSuccess)
            {
                // Окно ПРОСТО сообщает в эфир об удалении, а таблица сама выкинет его из UI! 🚀
                _eventBus.Publish(new UserDeletedMessage(id));
                ResetForm();

              
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
            ControlVisibility = Visibility.Collapsed;
            _eventBus.Publish(new FormVisibilityChangedMessage(GetType(), Visibility.Collapsed));
        }
        #endregion

        #region Вспомогательная логика
        private void InitEventBus()
        {
            _eventBus.Publish(new FormVisibilityChangedMessage(GetType(), Visibility.Collapsed));

            // Если открылось ДРУГОЕ окно — наше тихонько схлопывается
            _eventBus.Subscribe<FormVisibilityChangedMessage>(msg =>
            {
                if (msg.FormType != GetType() && msg.Visibility == Visibility.Visible)
                {
                    ResetForm();
                    ControlVisibility = Visibility.Collapsed;
                }
            });   
        }

        private void ResetForm()
        {
            TargetUserId = string.Empty;
            StatusMessage = "Введите ID ";
            CanDelete = false;
            SelectedUser = null;
        }
        #endregion
    }
}

