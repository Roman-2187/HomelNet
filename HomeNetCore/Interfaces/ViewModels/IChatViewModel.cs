namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IChatViewModel
    {
        public record NewSent(string Text, int ReceiverId);
        public record Send(int SenderId, string Text, string ChatType, int? TargetId = null, string? FilePath = null);
        public record Received(int MessageId, int SenderId, string Text, string ChatType, string? FilePath = null);
    }
}
