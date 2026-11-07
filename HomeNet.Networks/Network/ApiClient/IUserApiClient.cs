using HomeNet.Models.Shared.Entity;

namespace HomeNet.Network.Network.ApiClient
{
    public interface IUserApiClient : IApiClientBase
    {
        Task<bool> PushUserAsync(UserEntity user);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<IReadOnlyCollection<UserEntity>> FetchFreshUsersAsync(Guid deviceId, DateTimeOffset lastSyncedAt);

        // 🔥 Новые методы для работы с сервером (чтение и поиск)
        Task<UserEntity?> GetUserByIdAsync(Guid id);
        Task<IReadOnlyCollection<UserEntity>> GetAllUsersAsync();

        Task<UserEntity?> AuthenticateAsync(string email, string password);

        // 🔥 ФАКТЫ, которые публикует сам API-клиент
        public record UserRegisteredOnServer(UserEntity User);
        public record UserRegistrationFailed(Guid UserId, string Error);

        public record UserDeletedOnServer(Guid UserId);
        public record UserDeleteFailed(Guid UserId, string Error);
    }
}