namespace HomeNetCore.Exeptions
{
    [Serializable]
    public class SchemaProviderException : Exception
    {
        public SchemaProviderException()
        {
        }

        public SchemaProviderException(string? message) : base(message)
        {
        }

        public SchemaProviderException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
