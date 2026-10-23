using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeNetCore.Models.Entity
{
    public partial class MessageEntity : BaseEntity
    {
        [ObservableProperty] private Guid _senderId;
        [ObservableProperty] private Guid _receiverId;
        [ObservableProperty] private string _text = string.Empty;
        [ObservableProperty] private string _mediaType = "Text";
        [ObservableProperty] private string? _filePath;
        [ObservableProperty] private bool _isRead;

        [NotMapped][ObservableProperty] private bool _isOutgoing;

        [NotMapped]
        public string FormattedTime => CreatedAt.ToLocalTime().ToString("HH:mm");
    }
}
