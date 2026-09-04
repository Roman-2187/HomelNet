using HomeNetCore.Models;
using HomeNetCore.Models.InputUserData;
using HomeNetCore.Services.UsersServices;

namespace HomeNetCore.Interfaces
{
    public interface IRegisterService
    {
        Task<(bool IsSuccess, List<ValidationResult> Messages, UserEntity? CreatedUser)> RegisterUserAsync(CreateUserInput userInput);
    }
}