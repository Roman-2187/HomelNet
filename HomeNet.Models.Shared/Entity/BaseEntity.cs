using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNet.Models.Shared.Entity
{
    /// <summary>
    /// Реактивная базовая сущность. Поля создания и обновления реактивны для UI, 
    /// а чисто служебные флаги ОРМ и репликации оставлены обычными свойствами.
    /// </summary>
    public abstract partial class BaseEntity : ObservableObject
    {
        [ObservableProperty]
        private Guid _id = Guid.NewGuid();

        // 🎯 Чисто служебный флаг для ORM и репозиториев (0 = грязный, 1 = засинхронен)
        public int IsSynced { get; set; } = 0;

        // 🔥 ВОЗВРАЩАЕМ НА МЕСТО: Служебное поле версии для триггеров PostgreSQL!
        public long Version { get; set; } = 0;

        // --- ⏳ Раздельное время создания ---

        // Когда автор физически создал запись на своем устройстве
        [ObservableProperty]
        private DateTimeOffset _localCreatedAt = DateTimeOffset.UtcNow;

        // When база данных (сервер) закоммитила эту запись у себя
        [ObservableProperty]
        private DateTimeOffset _serverCreatedAt = DateTimeOffset.UtcNow;

        // --- 🔄 Единое общее время обновления ---

        [ObservableProperty]
        private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;
    }
}
