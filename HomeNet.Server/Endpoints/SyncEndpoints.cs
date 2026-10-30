using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Extensions;

namespace HomeNet.Server.Endpoints
{
    public static class SyncEndpoints
    {
        public static void MapSyncEndpoints(this IEndpointRouteBuilder endpoints, IDeviceSyncRepository deviceSyncRepository, HomeNetCore.Interfaces.Diagnostics.ILogger serverLogger)
        {
            var group = endpoints.MapGroup("/api/sync");

            // ============================================================================
            // 📥 ВЕКТОР СИНХРОНИЗАЦИИ: Получаем ID пользователя, привязанного к этому девайсу
            // ============================================================================
            group.MapGet("/version/{deviceId:guid}", async (Guid deviceId) =>
            {
                serverLogger.LogInfo($"[API SYNC] Запрос вектора синхронизации для девайса: {deviceId}");

                // Твой метод возвращает чистый Guid юзера!
                Guid userId = await deviceSyncRepository.GetUserIdByDeviceAsync(deviceId);

                // Если девайс новый и юзер не найден, вернется Guid.Empty
                return Results.Ok(new { DeviceId = deviceId, UserId = userId });
            });

            // ============================================================================
            // 📤 ФИКСАЦИЯ ЯКОРЯ СИНХРОНИЗАЦИИ: Обновляем чекпоинт устройства в Postgres
            // ============================================================================
            group.MapPost("/anchor", async (DeviceSyncRequest request) =>
            {
                if (request == null || request.DeviceId == Guid.Empty || request.UserId == Guid.Empty)
                    return Results.BadRequest("Неполные данные для фиксации якоря.");

                serverLogger.LogInfo($"[API SYNC] Фиксация якоря репликации для девайса: {request.DeviceId}");

                // Вызываем твой метод с тремя четкими параметрами!
                await deviceSyncRepository.UpdateDeviceCheckpointAsync(
                    request.DeviceId,
                    request.UserId,
                    DateTimeOffset.UtcNow
                );

                return Results.Ok(new { Anchored = true });
            });
        }
    }

    /// <summary>
    /// Транспортный DTO-рекорд для приема JSON-пакета на эндпоинте /anchor
    /// </summary>
    public record DeviceSyncRequest(Guid DeviceId, Guid UserId);
}
