using HomeNetCore.Enums.Navigation;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IAdminVm
    {
        // 🔥 НАМЕРЕНИЕ ТАЙТЛБАРА: Просьба включить режим админа
        public record RequestAdminZone;

        public record LogoutAdmin;
        public record AdminAuthenticated;
        public record OpenTableUsers;
        public record OpenSeedUsers;
        public record OpenDelete;
        public record OpenLogger;
        public record OpenEventInspector;
        public record AdminTabChanged(AdminSubTab ActiveTab);
    }
}
