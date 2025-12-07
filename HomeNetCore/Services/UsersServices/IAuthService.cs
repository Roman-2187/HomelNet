namespace HomeNetCore.Services.UsersServices
{
    public interface IAuthService
    {
        Task<(bool success, string? userName)> LoginAsync(string email, string password);
        bool ValidateEmailFormat(string email);
    }

   
   



}
