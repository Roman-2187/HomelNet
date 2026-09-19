using HomeNetCore.Models;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IAuthenticationViewModel
    {
        public record UserLogged(UserEntity User, bool IsFromAdminPanel = false);
    }
}
