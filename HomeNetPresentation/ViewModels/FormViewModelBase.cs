using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums;
using HomeNetCore.Messaging; // Подключаем наши чистые рекорды из Ядра
using HomeNetCore.Models.Validation;
using HomeNetServices.Services.Messaging; // Твой универсальный автобус из Сервисов

namespace HomeNetPresentation.ViewModels
{
    public abstract partial class FormViewModelBase : ObservableObject
    {
        // ГЛОБАЛЬНЫЙ СИГНАЛ ЛОГАУТА
        public static Action? OnGlobalResetRequested;

        // Защищенное поле автобуса для дочерних форм
        protected readonly IEventBus _eventBus;

        // Один кабель на весь проект! Все дочерние формы теперь имеют доступ к шине
        public IEventBus EventBus => _eventBus;

        // Конструктор
        public FormViewModelBase(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            OnGlobalResetRequested += ResetSession;
        }

        public void ResetSession()
        {
            IsComplete = false;
            IsCancelled = false;
            IsControlVisible = false; // <-- Чистый сброс видимости через bool! 🧼
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

        // 🔥 ЧИСТЫЙ ФЛАГ ВИДИМОСТИ: Заменил Visibility на bool и перевел в атрибут! 🧼
        // Toolkit сам создаст публичное свойство IsControlVisible
        [ObservableProperty]
        private bool _isControlVisible = false;

        // Магия Toolkit: этот метод автоматически вызывается СРАЗУ после изменения _isControlVisible
        partial void OnIsControlVisibleChanged(bool value)
        {
            // Публикуем наше очищенное от WPF сообщение (передаем текущий тип и bool флаг!)
            _eventBus.Publish(this, new FormVisibilityChangedMessage(this.GetType(), value));
        }

        [ObservableProperty]
        private bool _isCancelled = false;

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

        public void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results.ToDictionary(r => r.Field, r => r);
        }
    }
}


