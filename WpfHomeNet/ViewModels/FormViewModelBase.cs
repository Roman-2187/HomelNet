using HomeNetCore.Enums;
using HomeNetCore.Services.UsersServices;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WpfHomeNet.Messaging; 

namespace WpfHomeNet.ViewModels
{
    public abstract class FormViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
    

        // ГЛОБАЛЬНЫЙ СИГНАЛ ЛОГАУТА
        public static Action? OnGlobalResetRequested;

        // Делаем защищенное поле автобуса, чтобы все дочерние формы имели к нему прямой доступ
        protected readonly EventBus _eventBus;

        // Конструктор теперь принимает автобус
        public FormViewModelBase(EventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            OnGlobalResetRequested += ResetSession;
        }

        public void ResetSession()
        {
            IsComplete = false;
            ControlVisibility = Visibility.Collapsed;
            OnPropertyChanged(nameof(IsComplete));
            OnResetForm();
        }

        protected virtual void OnResetForm() { }

        private List<ValidationResult>? _validationResult;
        public List<ValidationResult> ValidationResult
        {
            get => _validationResult ?? throw new InvalidOperationException("пустая коллекция");
            set => _validationResult = value;
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isOpen;
        public bool IsOpen
        {
            get => _isOpen;
            set => SetField(ref _isOpen, value);
        }

        private bool _isComplete = false;
        public bool IsComplete
        {
            get => _isComplete;
            protected set => SetField(ref _isComplete, value);
        }

        private Visibility _controlVisibility = Visibility.Collapsed;
        public virtual Visibility ControlVisibility
        {
            get => _controlVisibility;
            set
            {
                if (SetField(ref _controlVisibility, value))
                {              
                    _eventBus.Publish(new FormVisibilityChangedMessage(this.GetType(), value));
                }
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            protected set => SetField(ref _statusMessage, value);
        }

        private string _submitButtonText = "Выполнить";
        public string SubmitButtonText
        {
            get => _submitButtonText;
            protected set => SetField(ref _submitButtonText, value);
        }

        private IReadOnlyDictionary<TypeField, ValidationResult> _validationResults
            = new Dictionary<TypeField, ValidationResult>();
        public IReadOnlyDictionary<TypeField, ValidationResult> ValidationResults
        {
            get => _validationResults;
            protected set => SetField(ref _validationResults, value);
        }

        public void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results.ToDictionary(r => r.Field, r => r);
        }
    }
}


