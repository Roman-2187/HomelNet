using System;

namespace HomeNetCore.Models
{
    public class MessageEntity
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string? Text { get; set; }
        public string? MediaType { get; set; } = "Text";
        public string? CloudUrl { get; set; }
        public string? LocalPath { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}