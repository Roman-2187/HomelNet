namespace HomeNetCore.Helpers.Exeptions
{
    [Serializable]
    public class DuplicateEmailException : Exception
    {
        public string Email { get; }

        public DuplicateEmailException(string email)
            : this($"Email {email} уже существует", email)
        {
        }

        public DuplicateEmailException(string email, Exception innerException)
            : this($"Email {email} уже существует", email, innerException)
        {
        }

        public DuplicateEmailException(string message, string email)
            : base(message)
        {
            Email = email;
        }

        public DuplicateEmailException(string message, string email, Exception innerException)
            : base(message, innerException)
        {
            Email = email;
        }
    }

}