using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Models.Validation;
using HomeNetPresentation.Services; // 🔥 Подключили пространство навигатора

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Стерильный базовый фундамент для всех интерактивных форм проекта SiberNet.
    /// Автоматически предоставляет доступ к шине и глобальному командиру навигации.
    /// </summary>
    public abstract partial class FormViewModelBase : ObservableObject
    {
        protected readonly IEventBus _eventBus;
        protected readonly NavigationStateManager _navigation; // 🔥 Спрятали командира в фундамент базы!

        public IEventBus EventBus => _eventBus;

        // 🔥 ПУБЛИЧНЫЙ МОСТ: Теперь ЛЮБАЯ дочерняя вьюмодель может читать энумы и текущего юзера!
        public NavigationStateManager Navigation => _navigation;

        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private string _submitButtonText = "Выполнить";

        [ObservableProperty]
        private IReadOnlyDictionary<TypeField, ValidationResult> _validationResults
            = new Dictionary<TypeField, ValidationResult>();

        // 🔥 Конструктор теперь принимает ДВА главных силовых кабеля системы
        public FormViewModelBase(IEventBus eventBus, NavigationStateManager navigationManager)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _navigation = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        }

        public void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results.ToDictionary(r => r.Field, r => r);
        }
    }
}
