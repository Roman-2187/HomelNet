using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetCore.Models
{
    /// <summary>
    /// 🔥 РЕАКТИВНАЯ СУЩНОСТЬ СООБЩЕНИЯ SiberNet
    /// Идеально мапится в Entity Framework (Postgres/SQLite) и автоматически обновляет UI WPF!
    /// </summary>
    public partial class MessageEntity : ObservableObject
    {
        [Key] // Главный ключ для EF Core
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

        [ObservableProperty]
        private DateTime _createdAt = DateTime.UtcNow;

        // --- Хитрые свойства для UI (Вычисляемые на лету) ---

        [NotMapped] // База данных (Postgres/SQLite) проигнорирует эти свойства
        public bool IsOutgoing => true; // Тут потом сделаешь проверку (SenderId == CurrentUserId)

        [NotMapped]
        public string FormattedTime => CreatedAt.ToLocalTime().ToString("HH:mm");
    }
}
