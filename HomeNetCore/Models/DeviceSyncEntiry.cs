using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetCore.Models
{
    /// <summary>
    /// Сущность для локальной таблицы DeviceSync. 
    /// Сюда смотрит твой ОРМ-билдер!
    /// </summary>
    public partial class DeviceSyncEntity : ObservableObject
    {
        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private Guid _deviceId;

        [ObservableProperty]
        private Guid _userId;

        [ObservableProperty]
        private DateTimeOffset _lastSyncedAt = DateTimeOffset.MinValue;

        [ObservableProperty]
        private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;
    }
}

