using HomeNetCore.Data.Interfaces;
using HomeNetCore.Enums;
using HomeNetCore.Models;
using HomeNetCore.Models.InputUserData;
using HomeNetCore.Services;
using HomeNetCore.Services.UsersServices;
using System.Windows;
using System.Windows.Input;

namespace WpfHomeNet.ViewModels
{
    public class RegistrationViewModel : FormViewModelBase
    {
        private readonly RegisterService _registerService;
        private MainViewModel? _mainViewModel;
        private UserEntity? _createdUser;

        public CreateUserInput UserData { get; set; } = new();
        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ToggleRegistrationCommand { get; }
       

        // Сервис регистрации теперь прилетает напрямую из контейнера!
        public RegistrationViewModel(RegisterService registerService)
        {
            _registerService = registerService ?? throw new ArgumentNullException(nameof(registerService));
          

            InitializeInitialHints();

            RegisterCommand = new RelayCommand(
                execute: async (obj) => await ExecuteRegisterCommand()
            );

            CancelCommand = new RelayCommand(
                execute: (obj) => CloseForm()
            );

            ToggleRegistrationCommand = new RelayCommand(
                execute: async (parameter) =>
                {
                    if (!IsComplete)
                        await ExecuteRegisterCommand();
                    else
                        CloseForm();
                },
                canExecute: (parameter) => !IsComplete || true
            );
        }

        private void InitializeInitialHints()
        {
            var initialHints = new List<ValidationResult>
            {
                new(TypeField.EmailType, "Введите email например 'User@example.com'", ValidationState.Info, true),
                new(TypeField.PasswordType, "Пароль должен содержать 8 символов буквы и цифры", ValidationState.Info, true),
                new(TypeField.NameType, "Имя пользователя должно содержать 3 буквы подряд", ValidationState.Info, true),
                new(TypeField.ConfirmedPasswordType, "Пароли должны совпадать", ValidationState.Info, true)
            };

            UpdateValidation(initialHints);
            SubmitButtonText = "Зарегистрироваться";
        }

        public void ConnectToMainViewModel(MainViewModel mainVm) => _mainViewModel = mainVm;

        private void CloseForm()
        {
            UserData = new();
            OnPropertyChanged(nameof(UserData));
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            InitializeInitialHints();
            ControlVisibility = Visibility.Collapsed;
        }

        private async Task ExecuteRegisterCommand()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                (IsComplete, ValidationResult, _createdUser) = await _registerService.RegisterUserAsync(UserData);
                ValidationResults = ValidationResult.ToDictionary(r => r.Field, r => r);

                if (IsComplete)
                {
                    StatusMessage = "Вы успешно зарегистрированы";
                    SubmitButtonText = "Завершить";

                    if (_createdUser != null)
                    {
                        _mainViewModel?.AddUserAction?.Invoke(_createdUser);
                    }
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При регистрации произошла ошибка: {ex.Message}";
            }
        }
    }
}