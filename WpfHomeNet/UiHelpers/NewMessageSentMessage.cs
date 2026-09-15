namespace WpfHomeNet.UiHelpers
{
    // Класс-посылка для шины сообщений 📦
    public class NewMessageSentMessage
    {
        public string Text { get; }
        public int ReceiverId { get; }

        public NewMessageSentMessage(string text, int receiverId)
        {
            Text = text;
            ReceiverId = receiverId;
        }
    }
}
