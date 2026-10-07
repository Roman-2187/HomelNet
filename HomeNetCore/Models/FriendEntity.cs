using System;

namespace HomeNetCore.Models
{
    public class FriendEntity
    {
        // 🎯 СУРРОГАТНЫЙ ПЕРВИЧНЫЙ КЛЮЧ: Спасёт SQLite от ошибки "more than one primary key"
        public Guid Id { get; set; } = Guid.NewGuid();

        // Идентификаторы пользователей теперь СТРОГО Guid ⚡
        public Guid UserId { get; set; }      // Кто добавил
        public Guid FriendId { get; set; }    // Кого добавил

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔥 ФИКС ТИПОВ СИНХРОНИЗАЦИИ: Никаких object?, только жесткий бэкенд-стандарт!
        public int IsSynced { get; set; } = 1;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
