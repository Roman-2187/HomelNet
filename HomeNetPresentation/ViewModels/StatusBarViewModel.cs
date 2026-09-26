using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    // 🔥 Реализуем IDisposable, чтобы синглтон-статусбар не копил мертвые подписки
    public partial class StatusBarViewModel : FormViewModelBase, IDisposable
    {
        [ObservableProperty]
        private string _statusText = "Готов к работе";

        public StatusBarViewModel(IEventBus eventBus, NavigationStateManager navigation)
            : base(eventBus, navigation)
        {
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // 🔥 ЧИСТОТА: Никаких лямбд, только ссылки на именованные методы! 🧼
            EventBus.Subscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            EventBus.Subscribe<IUsersTableViewModel.Refreshed>(OnUsersTableRefreshed);
        }

        #region 🎧 ИМЕНОВАННЫЕ МЕТОДЫ ПОДПИСОК (Для идеального графа в Инспекторе) 🧼

        private async void OnTextChanged(IStatusBarViewModel.TextChanged msg)
        {
            if (msg == null) return;

            // Просто и плавно обновляем текст на тот, который реально прилетел в посылке
            await UpdateStatusTextAsync(msg.NewStatus);
        }

        private void OnUsersTableRefreshed(IUsersTableViewModel.Refreshed msg)
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
        private async Task UpdateStatusTextAsync(string newText)
        {
            StatusText = newText;

            // Если захочешь сделать эффект "мигания" или временного статуса, 
            // можно подержать текст на экране и вернуть дефолтный "Готов к работе"
            // await Task.Delay(3000);
            // StatusText = "Готов к работе";
        }

        #region 🛡️ ЖЕЛЕЗОБЕТОННЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public void Dispose()
        {
            EventBus.Unsubscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            EventBus.Unsubscribe<IUsersTableViewModel.Refreshed>(OnUsersTableRefreshed);
        }

        #endregion
    }
}
