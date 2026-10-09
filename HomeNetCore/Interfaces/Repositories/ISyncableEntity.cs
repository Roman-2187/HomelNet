namespace HomeNetCore.Interfaces.Repositories
{
    /// <summary>
    /// Контракт сквозной синхронизации для дженерик-расширений ОРМ SiberNet.
    /// Гарантирует наличие ключевых полей для репликации.
    /// </summary>
    public interface ISyncableEntity
    {
        // Уникальный идентификатор записи в СУБД (Guid)
        Guid Id { get; set; }

        // Флаг состояния оффлайн-синхронизации (0 - изменен локально, 1 - синхронизирован)
        int IsSynced { get; set; }
    }
}
