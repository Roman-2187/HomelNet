using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetCore.Models
{
    /// <summary>
    /// 🔥 РЕАКТИВНАЯ СУЩНОСТЬ СВЯЗИ ДРУЗЕЙ SiberNet
    /// </summary>
    public partial class FriendEntity : ObservableObject
    {
        // 🎯 СУРРОГАТНЫЙ ПЕРВИЧНЫЙ КЛЮЧ: Идеально для распределенной синхронизации
        [ObservableProperty]
        private Guid _id = Guid.NewGuid();

        // Идентификаторы пользователей теперь СТРОГО Guid ⚡
        [ObservableProperty]
        private Guid _userId;      // Кто добавил

        [ObservableProperty]
        private Guid _friendId;    // Кого добавил

        // ⏱ ТОТАЛЬНЫЙ DateTimeOffset: Дата добавления в друзья со смещением
        [ObservableProperty]
        private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

        // 🎯 СИНХРО-ФЛАГ: Теперь реактивный! 
        [ObservableProperty]
        private int _isSynced = 0; // По умолчанию 0 (новая локальная запись)

        // ⏱ Высокоточная временная метка изменения (Last Write Wins)
        [ObservableProperty]
        private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;
    }
}
