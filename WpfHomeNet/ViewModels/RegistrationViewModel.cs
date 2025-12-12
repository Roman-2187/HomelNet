using HomeNetCore.Data.Interfaces;
using HomeNetCore.Services;
using HomeNetCore.Services.UsersServices;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace WpfHomeNet.ViewModels
{
    public class RegistrationViewModel : INotifyPropertyChanged
    {
        #region поля и переменные
        private readonly RegisterService _registerService;
        private readonly IUserRepository? _userRepository;

        public CreateUserInput UserData { get; set; } = new();
        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ToggleRegistrationCommand { get; }

        #endregion


        public RegistrationViewModel(IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _registerService = new RegisterService(_userRepository);

            RegisterCommand = new RelayCommand(
                execute: async (obj) => await ExecuteRegisterCommand(),
                canExecute: (obj) => true
            );

            CancelCommand = new RelayCommand(
                execute: (obj) =>
                {
                    ResetRegistrationForm();
                    ControlVisibility = Visibility.Collapsed;
                },
                canExecute: (obj) => true
            );

            ToggleRegistrationCommand = new RelayCommand(
                execute: async (parameter) =>
                {
                    if (!IsRegistrationComplete)
                        await ExecuteRegisterCommand();
                    else
                    {
                        ResetRegistrationForm();
                        ControlVisibility = Visibility.Collapsed;
                    }
                },
                canExecute: (parameter) => !IsRegistrationComplete || true
            );
        }


        #region свойсва  управления регистрацией

        private bool _isRegistrationComplete;
        public bool IsRegistrationComplete
        {
            get => _isRegistrationComplete;
            private set => SetField(ref _isRegistrationComplete, value);
        }

        private Visibility _controlVisibility = Visibility.Collapsed;
        public Visibility ControlVisibility
        {
            get => _controlVisibility;
            set => SetField(ref _controlVisibility, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        private string _registerButtonText = "Зарегистрироваться";
        public string RegisterButtonText
        {
            get => _registerButtonText;
            private set => SetField(ref _registerButtonText, value);
        }

        private bool _areFieldsEnabled = true;
        public bool AreFieldsEnabled
        {
            get => _areFieldsEnabled;
            private set => SetField(ref _areFieldsEnabled, value);
        }

        private IReadOnlyDictionary<TypeField, ValidationResult> _validationResults = new Dictionary<TypeField, ValidationResult>();
        public IReadOnlyDictionary<TypeField, ValidationResult> ValidationResults
        {
            get => _validationResults;
            private set => SetField(ref _validationResults, value);
        }

        #endregion



        #region методы формы регистрации

        public void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results.ToDictionary(r => r.Field, r => r);
        }

        private void ResetRegistrationForm()
        {
            UserData = new();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            AreFieldsEnabled = true;
            RegisterButtonText = "Зарегистрироваться";
            IsRegistrationComplete = false;
        }

        private async Task ExecuteRegisterCommand()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                var (isSuccess, messages) = await _registerService.RegisterUserAsync(UserData);
                ValidationResults = messages.ToDictionary(r => r.Field, r => r);

                if (isSuccess)
                {
                    StatusMessage = "Вы успешно зарегистрированы";
                    AreFieldsEnabled = false;
                    IsRegistrationComplete = true;
                    RegisterButtonText = "Выйти";
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При регистрации произошла ошибка: {ex.Message}";
                AreFieldsEnabled = true;
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

        #endregion

}



