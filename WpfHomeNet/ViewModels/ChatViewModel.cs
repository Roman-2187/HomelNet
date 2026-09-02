using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using WpfHomeNet.Messaging;

namespace WpfHomeNet.ViewModels
{
    // Мозги для нашего суверенного чата! 🧠🚀
    public partial class ChatViewModel : ObservableObject
    {
        private readonly EventBus _eventBus;

        // Поле ввода сообщения, привязанное к TextBox в XAML
        [ObservableProperty]
        private string _messageText = string.Empty;

        public ChatViewModel(EventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        // Команда отправки эсэмэски брату
        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText)) return;

            string textToSend = MessageText;
            MessageText = string.Empty; // Сразу чистим поле ввода, чтобы интерфейс летал! 🧼

            // 📢 Пуляем сообщение в наш "автобус"! 
            // Локальная SQLite поймает его, запишет на диск, а когда прикрутим Postgres — оно улетит на сервер
            // _eventBus.Publish(new NewMessageSentMessage(textToSend));

            await Task.CompletedTask;
        }

        // Команда для кнопки "Скрепка" 📎
        [RelayCommand]
        private void AttachFile()
        {
            // Здесь будет выбор файла с диска, который мы потом отправим брату
        }
    }
}

