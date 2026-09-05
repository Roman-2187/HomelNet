using HomeNetCore.Models;
using HomeNetCore.Services;
using HomeNetCore.Services.UsersServices;
using HomeNetCore.Models.Validation;
namespace HomeNetCore.Interfaces
{
    public interface IRegisterService
    {
        public  Task<RegistrationVerdict> RegisterUserAsync(UserEntity user);
    }
}