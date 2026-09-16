using HomeNetCore.Models;
using HomeNetCore.Models.Validation;
namespace HomeNetOrm.Interfaces
{
    public interface IRegisterService
    {
        public  Task<RegistrationVerdict> RegisterUserAsync(UserEntity user);
    }
}