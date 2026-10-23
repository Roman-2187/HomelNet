namespace HomeNetCore.Models.Entity
{
  
    /// <summary>
    /// Полностью самостоятельная техническая сущность для таблицы синхронизации устройств.
    /// Никакого UI, никакого MVVM — чистый POCO-класс для работы базы данных.
    /// </summary>
    public class DeviceSyncEntity
    {
        // Составной первичный ключ (DeviceId + UserId)
        public Guid DeviceId { get; set; }
        public Guid UserId { get; set; }

        // Серверная версия изменений (BIGINT для триггера в Postgres)
        public long Version { get; set; } = 0;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

