using HomeNetCore.Extensions;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels; // Подключаем интерфейсы для рефреша
using HomeNetOrm.Interfaces; // 🔥 ДОБАВЛЯЕМ: Чтобы MainViewModel видела контракт твоего инициализатора базы
using HomeNetPresentation.Services;
using System;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel
    {
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus; // Сохраняем шину в поле класса

        public MainViewModel(ILogger logger, IEventBus eventBus, NavigationStateManager navigation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus)); // Валидируем шину

            // 🔥 БЛИН-ТЕСТ: Заставляем бэкенд намертво бахнуть строкой в логгер при старте!
            _logger.LogError("=== [СИСТЕМА SIBERNET ЗАПУЩЕНА]: ТЕСТ КИБЕРПАНК ЛОГГЕРА ===");

            // 🎯 РЕАКТИВНАЯ ЛОВУШКА: Вместо слепого выстрела в пустоту, садимся в засаду автобуса!
            // Ждем, пока IDbInitializer отчитается о полной проверке и накате всех таблиц.
            _eventBus.Subscribe<ISchemaSqlInitializer.DatabaseReady>(OnDatabaseInfrastructureReady);
        }

        // 🦾 КИБЕР-БУДИЛЬНИК: Сработает строго ПОСЛЕ лога "ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ ЗАВЕРШЕНА"
        private void OnDatabaseInfrastructureReady(ISchemaSqlInitializer.DatabaseReady msg)
        {
            _logger.LogInfo("[MainViewModel] База данных подала сигнал готовности. Пинаем таблицу юзеров на прогрев...");

            // Вот теперь этот выстрел безопасен на 100%! Инфраструктура готова, генераторы перепеклись.
            // Сигнал поймает TableUsersViewModel, зайдет в базу и чисто скачает кэш без NullReferenceException!
            _eventBus.Publish(this, new IUsersTableVm.RefreshRequest());
        }
    }
}
