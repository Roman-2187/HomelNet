namespace HomeNetCore.Interfaces.Repositories
{
    public interface IDeviceSyncRepository
    {
        // Вытащить UserId по DeviceId
        Task<Guid> GetUserIdByDeviceAsync(Guid deviceId);

        // Сдвинуть маркер времени (тот самый UPSERT)
        Task UpdateDeviceCheckpointAsync(Guid deviceId, Guid userId, DateTimeOffset now);
    }
}

