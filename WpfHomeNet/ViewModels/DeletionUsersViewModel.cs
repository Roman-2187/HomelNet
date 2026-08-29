using HomeNetCore.Models;
using HomeNetCore.Services.DeleteService;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    public class DeletionUsersViewModel : FormViewModelBase
    {
        #region Поля и переменные
        private readonly DeleteService _deleteService;
        private readonly EventBus _eventBus; 

        private ObservableCollection<UserEntity>? _mainUsersList;
        private UserEntity? _selectedUser;
        private string _targetUserId = string.Empty;
        private bool _canDelete;
        #endregion

        #region Свойства и Команды
        public ObservableCollection<UserEntity>? MainUsersList
        {
            get => _mainUsersList;
            set => SetField(ref _mainUsersList, value);
        }

        public UserEntity? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetField(ref _selectedUser, value) && _selectedUser != null)
                {
                    // Автоматически переносим ID выбранного юзера в поле поиска
                    TargetUserId = _selectedUser.DisplayInfo.ToString();
                    CanDelete = true;
                }
            }
        }

        public string TargetUserId
        {
            get => _targetUserId;
            set
            {
                if (SetField(ref _targetUserId, value))
                {
                    // Сообщаем кнопке поиска, что текст изменился
                    (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();

                    // ХИТРЫЙ ХАК: Если юзер ввёл ID руками, проверяем, можно ли активировать кнопку удаления
                    if (int.TryParse(_targetUserId, out int parsedId) && parsedId > 0)
                    {
                        CanDelete = true;
                    }
                    else if (string.IsNullOrWhiteSpace(_targetUserId))
                    {
                        CanDelete = false;
                    }

                    // Принудительно заставляем кнопку удаления пересчитать свой статус CanExecute!
                    (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanDelete
        {
            get => _canDelete;
            set
            {
                if (SetField(ref _canDelete, value))
                {
                    // Как только флаг меняется — кнопка удаления мгновенно разблокируется в UI!
                    (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion


        #region Конструктор
        public DeletionUsersViewModel(DeleteService deleteService, EventBus eventBus)
        {
            _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            ControlVisibility = Visibility.Collapsed;
            SubmitButtonText = "Удалить";
            StatusMessage = "Введите ID ";

            SearchCommand = new RelayCommand(
                execute: async (_) => await ExecuteSearchCommandAsync(),
                canExecute: (_) => !string.IsNullOrWhiteSpace(TargetUserId)
            );

            DeleteCommand = new RelayCommand(
                execute: async (_) => await ExecuteDeleteCommandAsync(),
                canExecute: (_) => CanDelete
            );

            CancelCommand = new RelayCommand(
                execute: (_) =>
                {
                    ResetForm();
                    ControlVisibility = Visibility.Collapsed;
                    // Уведомляем автобус, что мы закрылись ручками
                    _eventBus.Publish(new FormVisibilityChangedMessage(this.GetType(), Visibility.Collapsed));
                }
            );

            // ПРАВКА СТАРТА: Сразу принудительно шлём в автобус сигнал, что мы скрыты!
            _eventBus.Publish(new FormVisibilityChangedMessage(this.GetType(), Visibility.Collapsed));

            // СЛУШАЕМ АВТОБУС: Если открылась любая ДРУГАЯ форма — мы мгновенно тушимся
            _eventBus.Subscribe<FormVisibilityChangedMessage>(msg =>
            {
                if (msg.FormType != this.GetType() && msg.Visibility == Visibility.Visible)
                {
                    ResetForm();
                    ControlVisibility = Visibility.Collapsed;
                }
            });
        }
        #endregion


        #region Логика работы
        private void ResetForm()
        {
            TargetUserId = string.Empty;
            StatusMessage = "Введите ID ";
            CanDelete = false;
            SelectedUser = null;
        }

        private async Task ExecuteSearchCommandAsync()
        {
            StatusMessage = "Поиск пользователя в базе данных...";
            CanDelete = false;

            var (isSuccess, message, _) = await _deleteService.SearchUserAsync(TargetUserId);

            StatusMessage = message;
            CanDelete = isSuccess;
        }

        private async Task ExecuteDeleteCommandAsync()
        {
            int id = _selectedUser?.Id ?? (int.TryParse(TargetUserId, out int parsedId) ? parsedId : -1);
            if (id == -1) return;

            StatusMessage = $"Удаление пользователя с ID {id}...";

            var (isSuccess, message) = await _deleteService.DeleteUserAsync(id);

            StatusMessage = message;

            if (isSuccess)
            {
                // ИСПРАВЛЕНИЕ: Вместо ручного экшена OnUserDeletedFromDb швыряем сообщение в автобус!
                _eventBus.Publish(new UserDeletedMessage(id));
                ResetForm();
            }
        }
        #endregion
    }
}
