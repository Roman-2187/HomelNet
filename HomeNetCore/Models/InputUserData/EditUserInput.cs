namespace HomeNetCore.Models.InputUserData
{
    public class EditUserInput : LoginInUserInput
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }


}
