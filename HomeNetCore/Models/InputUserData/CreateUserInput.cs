namespace HomeNetCore.Models.InputUserData
{

    public class CreateUserInput : LoginInUserInput
    {
        public string UserName { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }


}
