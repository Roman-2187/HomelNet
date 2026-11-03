using HomeNet.Models.Shared.Entity;

namespace HomeNetCore.Interfaces.ViewModels.AdminZone
{
    public interface IUsersTableVm
    {
       
        public record Added(UserEntity User);
        public record Refreshed(IReadOnlyList<UserEntity> Users);

        // 🔥 ДОБАВИЛИ РЕКОРД ОБНОВЛЕНИЯ: Для уведомления UI о смене DeviceId или IsSynced
        public record Updated(UserEntity User);
        public record RefreshRequest();
    }
}
