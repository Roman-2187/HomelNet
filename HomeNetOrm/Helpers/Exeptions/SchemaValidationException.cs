namespace HomeNetOrm.Helpers.Exeptions;
[Serializable]
internal class SchemaValidationException : Exception
{
    public SchemaValidationException()
    {
    }

    public SchemaValidationException(string? message) : base(message)
    {
    }

    public SchemaValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}