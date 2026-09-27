using System;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Глобальная строка состояния приложения SiberNet.
    /// Наследуется от дженерик-базы с указанием базового менеджера навигации верхнего уровня.
    /// </summary>
    public partial class StatusBarViewModel : FormViewModelBase<NavigationStateManager>, IDisposable
    {
        [ObservableProperty]
        private string _statusText = "Готов к работе";

        public StatusBarViewModel(IEventBus eventBus, NavigationStateManager navigation)
            : base(eventBus, navigation) // Передали базовый навигатор верхнего уровня
        {
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 🔥 ЧИСТОТА ДЛЯ ИНСПЕКТОРА: Никаких лямбд, только ссылки на именованные методы! 🧼
            _eventBus.Subscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersTableRefreshed);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private async void OnTextChanged(IStatusBarViewModel.TextChanged msg)
        {
            if (msg == null) return;

            // Просто и плавно обновляем текст на тот, который реально прилетел в посылке
            await UpdateStatusTextAsync(msg.NewStatus);
        }

        private void OnUsersTableRefreshed(IUsersTableVm.Refreshed msg)
        {
            if (msg == null) return;

            // Статус-бар реагирует на обновление таблицы и выводит красивый итог
            StatusText = msg.Users != null && msg.Users.Count > 0
                ? $"База данных синхронизирована. Загружено пользователей: {msg.Users.Count}"
                : "Синхронизация завершена. Список пользователей пуст.";
        }

        #endregion

        /// <summary>
        /// Плавно выставляет текст статуса без побочных эффектов и холостого спама в шину
        /// </summary>
        private Task UpdateStatusTextAsync(string newText)
        {
            StatusText = newText;
            return Task.CompletedTask;
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose(); // Чистим базовые ресурсы дженерик-базы
            _eventBus.Unsubscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersTableRefreshed);
        }

        #endregion
    }
}
