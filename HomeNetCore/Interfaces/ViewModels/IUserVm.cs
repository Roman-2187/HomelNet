using HomeNetCore.Enums.Navigation;
using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IUserVm
    {
        // Чёткие намерения пользователя для гостевой зоны и входа
        public record OpenAuth;          // Ткнули "Войти"
        public record OpenRegistration;  // Ткнули "Регистрация"
        public record BackToStart;       // Назад в абсолютный ноль (StartZone)
        // Передаем строго саму сущность залогиненного пользователя! 🧼
        public record UserAuthenticated(UserEntity User);
        public record OpenProfile;       // Открыть профиль внутри ClientZone
        public record OpenMessenger;     // Вернуться в мессенджер

        public record LogoutClient;

        // 🔥 ДОБАВИЛИ ФАКТ: Пуля от навигатора для уведомления UI!
        public record ClientTabChanged(ClientSubTab ActiveTab);

    }
}
