using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetCore.Models
{
    /// <summary>
    /// 🔥 РЕАКТИВНАЯ СУЩНОСТЬ СООБЩЕНИЯ SiberNet
    /// </summary>
    public partial class MessageEntity : ObservableObject
    {
        [Key]
        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private Guid _senderId;

        [ObservableProperty]
        private Guid _receiverId;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private string _mediaType = "Text"; // "Text", "File", "Image"

        [ObservableProperty]
        private string? _filePath;

        // 🔥 КРИТИЧНО ДЛЯ ГАЛОЧЕК ЧАТА: Изменение этого поля UI увидит мгновенно!
        [ObservableProperty]
        private bool _isRead;

        // ⏱ ТОТАЛЬНЫЙ DateTimeOffset: Точное время отправки сообщения
        [ObservableProperty]
        private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

        // ⏱ Высокоточная временная метка изменения (Last Write Wins)
        [ObservableProperty]
        private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;

        // 🎯 СИНХРО-ФЛАГ: Теперь реактивный. UI моментально покажет, что сообщение улетело на сервер
        [ObservableProperty]
        private int _isSynced = 0;

        // --- Хитрые свойства для UI (Вычисляемые на лету) ---

        [NotMapped]
        public bool IsOutgoing => true; // Тут потом сделаешь проверку (SenderId == CurrentUserId)

        [NotMapped]
        public string FormattedTime => CreatedAt.ToLocalTime().ToString("HH:mm");
    }
}
