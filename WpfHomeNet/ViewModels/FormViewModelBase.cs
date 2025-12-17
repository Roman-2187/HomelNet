using HomeNetCore.Enums;
using HomeNetCore.Services.UsersServices;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WpfHomeNet.ViewModels
{

    public abstract class FormViewModelBase : INotifyPropertyChanged
    {
        // События
        public event PropertyChangedEventHandler? PropertyChanged;


        private List<ValidationResult>? _validationResult;

        public List<ValidationResult> ValidationResult
        {
            get => _validationResult ?? throw new InvalidOperationException("пустая коллекция");
            set => _validationResult = value;
        }

        // Защищённый метод для установки полей с оповещением
        protected bool SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
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

        private bool _isOpen ;
        public bool IsOpen
        {
            get => _isOpen;
            set => SetField(ref _isOpen, value);
        }

        
        private bool _isComplete;
        public bool IsComplete
        {
            get => _isComplete;
            protected set => SetField(ref _isComplete, value);
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

        // Метод для обновления валидации
        public void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results.ToDictionary(r => r.Field, r => r);
        }
    }
}
