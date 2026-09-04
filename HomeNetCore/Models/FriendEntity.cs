using System;

namespace HomeNetCore.Models
{
    public class FriendEntity
    {
        // У этой таблицы нет одиночного ID, она просто связывает двух пользователей вместе! 👥
        public int UserId { get; set; }      // Кто добавил
        public int FriendId { get; set; }    // Кого добавил
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

