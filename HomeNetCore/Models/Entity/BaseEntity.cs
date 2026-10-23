using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetCore.Models.Entity
{
    /// <summary>
    /// Базовый класс с полями синхронизации. Хочешь — мапь в БД, хочешь — игнорируй.
    /// </summary>
    public abstract partial class BaseEntity : ObservableObject
    {
        [ObservableProperty]
        private Guid _id = Guid.NewGuid();

        // 🎯 Флаг для клиента (0 - изменен локально, 1 - синхронизирован)
        [ObservableProperty]
        private int _isSynced = 0;

        [ObservableProperty] private long _version = 0;


        [ObservableProperty]
        private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

        [ObservableProperty]
        private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;
    }
}
