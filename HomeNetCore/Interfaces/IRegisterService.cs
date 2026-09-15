using HomeNetCore.Messaging;
using HomeNetCore.Models;
namespace HomeNetOrm.Interfaces
{
    public interface IRegisterService
    {
        public  Task<RegistrationVerdict> RegisterUserAsync(UserEntity user);
    }
}