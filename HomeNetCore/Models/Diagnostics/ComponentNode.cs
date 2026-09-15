
namespace HomeNetCore.Models.Diagnostics
{
    /// <summary>
    /// Тот самый "выстрел" в шине. Описывает факт публикации сообщения.
    /// </summary>
    public record SignalEvent(
        DateTime Time,        // Точное время до миллисекунд, когда пролетел сигнал
        string Source,        // Имя класса-отправителя (например, "ChatViewModel")
        Type MessageType      // Системный "паспорт" самого сообщения (например, "ReceivedChatMessage")
    );

    /// <summary>
    /// Стрелочка связи. Описывает, кто и каким методом слушает воздух.
    /// </summary>
    public record SubscriptionLink(
        Type MessageType,     // На какой тип сообщения подписались
        string MethodName     // Имя метода, который сработает (например, "OnMessageReceived")
    );


    public class ComponentNode
    {
        public string Name { get; init; } = "DefaultComponent";
        // Хэш-сеты гарантируют уникальность связей без глупых проверок через if/contains
        public HashSet<Type> PublishedMessages { get; } = [];
        public HashSet<SubscriptionLink> Subscriptions { get; } = [];
    }

    
}


