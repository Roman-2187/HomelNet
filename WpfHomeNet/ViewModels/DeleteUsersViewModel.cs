using HomeNetCore.Models;
using HomeNetCore.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HomeNetCore.Services.DeleteService;





   

    namespace WpfHomeNet.ViewModels
    {
        public class DeleteUsersViewModel : FormViewModelBase
        {
            private readonly DeleteService _deleteService;
            private MainViewModel? _mainViewModel;

            private ObservableCollection<UserEntity>? _mainUsersList;
            public ObservableCollection<UserEntity>? MainUsersList
            {
                get => _mainUsersList;
                set => SetField(ref _mainUsersList, value); // Базовый метод мгновенно обновит XAML!
            }

            private UserEntity? _selectedUser;
            public UserEntity? SelectedUser
            {
                get => _selectedUser;
                set
                {
                    if (SetField(ref _selectedUser, value) && _selectedUser != null)
                    {
                        TargetUserId = _selectedUser.Id.ToString();
                        CanDelete = true;
                    }
                }
            }

            private string _targetUserId = string.Empty;
            public string TargetUserId
            {
                get => _targetUserId;
                set
                {
                    if (SetField(ref _targetUserId, value))
                    {
                        // Сообщаем кнопке поиска, что текст изменился
                        (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    }
                }
            }

            private bool _canDelete;
            public bool CanDelete
            {
                get => _canDelete;
                set => SetField(ref _canDelete, value);
            }

            public ICommand SearchCommand { get; }
            public ICommand DeleteCommand { get; }
            public ICommand CancelCommand { get; }
            public Action<int>? OnUserDeletedFromDb { get; internal set; }

            // Принимаем UserService, но внутри собираем наш чистый DeleteService
            public DeleteUsersViewModel(UserService userService)
            {
                if (userService == null) throw new ArgumentNullException(nameof(userService));
                _deleteService = new DeleteService(userService);

                ControlVisibility = Visibility.Collapsed;
                SubmitButtonText = "Удалить";
                StatusMessage = "Введите ID "; // Используем базовое свойство

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
                    }
                );
            }

            public void ConnectToMainViewModel(MainViewModel mainVm) => _mainViewModel = mainVm;

            private void ResetForm()
            {
                TargetUserId = string.Empty;
                StatusMessage = "Введите ID ";
                CanDelete = false;
                SelectedUser = null;
            }

            // Чистый асинхронный поиск без единого блока try-catch!
            private async Task ExecuteSearchCommandAsync()
            {
                StatusMessage = "Поиск пользователя в базе данных...";
                CanDelete = false;

                // Вызываем наш сервис ядра и забираем готовый кортеж результатов
                var (isSuccess, message, _) = await _deleteService.SearchUserAsync(TargetUserId);

                // Вью-модель просто выводит на экран то, что решило ядро
                StatusMessage = message;
                CanDelete = isSuccess;
            }

            // Чистое асинхронное удаление без шума и каши!
            private async Task ExecuteDeleteCommandAsync()
            {
                int id = _selectedUser?.Id ?? (int.TryParse(TargetUserId, out int parsedId) ? parsedId : -1);
                if (id == -1) return;

                StatusMessage = $"Удаление пользователя с ID {id}...";

                // Дёргаем метод удаления из ядра
                var (isSuccess, message) = await _deleteService.DeleteUserAsync(id);

                StatusMessage = message;

                if (isSuccess)
                {
                    OnUserDeletedFromDb?.Invoke(id); // Наш любимый телефонный провод-делегат
                    ResetForm(); // Чистим поля и сбрасываем выделение ListBox
                }
            }
        }
    }
