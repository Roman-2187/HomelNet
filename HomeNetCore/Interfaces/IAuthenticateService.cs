using HomeNetCore.Models;
using HomeNetCore.Models.Validation;

namespace HomeNetCore.Interfaces
{
    public interface IAuthenticateService
    {
        Task<(bool IsSuccess, List<ValidationResult> Messages, UserEntity? User)> CheckUserAsync(UserEntity userInput);
    }
}