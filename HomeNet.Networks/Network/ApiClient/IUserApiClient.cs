using HomeNet.Models.Shared.Entity;

namespace HomeNet.Network.Network.ApiClient
{
    public interface IUserApiClient : IApiClientBase
    {
        Task<bool> PushUserAsync(UserEntity user);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<IReadOnlyCollection<UserEntity>> FetchFreshUsersAsync(Guid deviceId, DateTimeOffset lastSyncedAt);
    }
}
