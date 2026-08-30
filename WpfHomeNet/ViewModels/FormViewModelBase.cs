using CommunityToolkit.Mvvm.ComponentModel; 
using HomeNetCore.Enums;
using HomeNetCore.Services.UsersServices;
using System.Windows;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
   
    public abstract partial class FormViewModelBase : ObservableObject
    {
        // ГЛОБАЛЬНЫЙ СИГНАЛ ЛОГАУТА
        public static Action? OnGlobalResetRequested;

        // Защищенное поле автобуса для дочерних форм
        protected readonly EventBus _eventBus;

        // Конструктор
        public FormViewModelBase(EventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            OnGlobalResetRequested += ResetSession;
        }

        public void ResetSession()
        {
            IsComplete = false; // <--- ТЕПЕРЬ ПИШЕМ С БОЛЬШОЙ БУКВЫ! 🧼
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

        #region АВТОМАТИЧЕСКИЕ СВОЙСТВА (Штамповочный цех Microsoft) 🦾

        [ObservableProperty]
        private bool _isOpen; 

        [ObservableProperty]
        private bool _isComplete = false; 

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _submitButtonText = "Выполнить"; 

        [ObservableProperty]
        private IReadOnlyDictionary<TypeField, ValidationResult> _validationResults
            = new Dictionary<TypeField, ValidationResult>();

        #endregion

        
        private Visibility _controlVisibility = Visibility.Collapsed;
        public virtual Visibility ControlVisibility
        {
            get => _controlVisibility;
            set
            {
                if (SetProperty(ref _controlVisibility, value))
                {
                    _eventBus.Publish(new FormVisibilityChangedMessage(this.GetType(), value));
                }
            }
        }

        public void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results.ToDictionary(r => r.Field, r => r);
        }
    }
}



