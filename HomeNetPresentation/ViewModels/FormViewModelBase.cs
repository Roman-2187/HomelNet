using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetCore.Models.Validation;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Стерильный generic-фундамент для всех интерактивных форм проекта SiberNet.
    /// Строго типизирует навигатор для каждого конкретного экрана.
    /// </summary>
    public abstract partial class FormViewModelBase<TNavigation> : ObservableObject, IDisposable, IFormViewModel
        where TNavigation : NavigationStateManager
    {
        protected readonly IEventBus _eventBus;
        protected readonly TNavigation _navigation; // 🔥 Строгий тип навигатора!

        public IEventBus EventBus => _eventBus;

        // 🔥 ИДЕАЛЬНЫЙ МОСТ: Наследник видит свойства СВОЕГО конкретного навигатора без кастов!
        public TNavigation Navigation => _navigation;

        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private string _submitButtonText = "Выполнить";

        [ObservableProperty]
        private IReadOnlyDictionary<TypeField, ValidationResult> _validationResults
            = new Dictionary<TypeField, ValidationResult>();

        public FormViewModelBase(IEventBus eventBus, TNavigation navigationManager)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _navigation = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        }

        public void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results.ToDictionary(r => r.Field, r => r);
        }

        /// <summary>
        /// Глобальный виртуальный стерилизатор для безопасной зачистки подписок в наследниках
        /// </summary>
        public virtual void Dispose()
        {
            // Базовая зачистка, если потребуется
        }
    }
}
