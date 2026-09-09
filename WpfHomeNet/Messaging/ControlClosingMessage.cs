namespace WpfHomeNet.Messaging
{
    // Простая радиограмма: сообщает тип контрола, который нужно отправить в космос
    public class ControlClosingMessage
    {
        public Type ControlType { get; }

        public ControlClosingMessage(Type controlType)
        {
            ControlType = controlType;
        }
    }
}
