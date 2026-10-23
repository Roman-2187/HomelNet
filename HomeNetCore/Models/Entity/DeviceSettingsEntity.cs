namespace HomeNetCore.Models.Entity
{
    
        /// <summary>
        /// Внутреннее хранилище сессии и автовхода (Только в SQLite).
        /// Хранит уникальный ID этого устройства и данные авторизованного юзера.
        /// </summary>
        public class DeviceSettingsEntity
        {
            // ==========================================
            // 🔐 ЕДИНЫЙ ИДЕНТИФИКАТОР (ТОЛЬКО ДЛЯ ЧТЕНИЯ)
            // ==========================================

            public Guid DeviceId { get; set; }

            public string Id { get; set; } = "current_session";


            // ==========================================
            // 🔄 ПЕРЕПИСЫВАЕМЫЕ ПОЛЯ (ДЛЯ АВТОВХОДА)
            // ==========================================

            // 🎯 СТАЛО NULLABLE (Добавили '?'): Теперь в базе будет разрешен NULL
            public Guid? UserId { get; set; }

            // 🎯 СТАЛО NULLABLE: Убираем "= string.Empty", пишем string?
            public string? Email { get; set; }

            // 🎯 СТАЛО NULLABLE: Пишем string? чтобы база не ругалась на пустой токен
            public string? Password { get; set; }

            public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        }
    





}
