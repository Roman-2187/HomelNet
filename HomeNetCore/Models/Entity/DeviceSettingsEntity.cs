
    
    namespace HomeNetCore.Models.Entity
    {
        /// <summary>
        /// Внутреннее хранилище сессии и автовхода (Только в SQLite).
        /// Хранит уникальный ID этого устройства и данные авторизованного юзера.
        /// </summary>
        public class DeviceSettingsEntity
        {
            // 🔐 ЕДИНЫЙ ИДЕНТИФИКАТОР И ПЕРВИЧНЫЙ КЛЮЧ
            public Guid DeviceId { get; set; }

            // ==========================================
            // 🔄 ПЕРЕПИСЫВАЕМЫЕ ПОЛЯ (ДЛЯ АВТОВХОДА)
            // ==========================================
            public Guid? UserId { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
            public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        }
    }








