public class DuplicateMessageException : Exception
{
    public Guid MessageId { get; }
    public DuplicateMessageException(Guid messageId)
        : base($"Сообщение с ID {messageId} уже существует в локальной базе.")
    {
        MessageId = messageId;
    }
}