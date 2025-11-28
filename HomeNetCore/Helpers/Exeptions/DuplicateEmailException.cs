namespace HomeNetCore.Helpers.Exceptions
{
    public class DuplicateEmailException : Exception
    {
        public string Email { get; }

        public DuplicateEmailException(string? email) : this($"Email {email} уже существует", email) { }


        public DuplicateEmailException(string email, Exception innerException)
            : this($"Email {email} уже существует", email, innerException) { }


        public DuplicateEmailException(string message, string email) : base(message)
        {
            Email = email;
        }

        public DuplicateEmailException(string message, string email, Exception innerException)
            : base(message, innerException)
        {
            Email = email;
        }

        public string GetExistEmail()
        {
            return $"Email {Email} уже существует.\n" + "- Используйте другой email";
                            
        }
    }



}