using HomeNetCore.Data.Builders; // Подключаем наш новый TableSchemaBuilder
using HomeNetCore.Data.Schemes;
using HomeNetCore.Models;

namespace HomeSocialNetwork.Core
{
    public static class SchemaRegistry
    {
        public static IEnumerable<TableSchema> GetAllSchemas()
        {
            // 1. СХЕМА ТАБЛИЦЫ ПОЛЬЗОВАТЕЛЕЙ (users)
            var users = new TableBuilder<UserEntity>("Users");
            users.AddColumn(u => u.Id).AsPrimaryKey().AsAutoIncrement();
            users.AddColumn(u => u.FirstName).HasLength(50).IsRequired();
            users.AddColumn(u => u.LastName).HasLength(50);
            users.AddColumn(u => u.PhoneNumber).HasLength(50);
            users.AddColumn(u => u.Email).HasLength(50).IsRequired().IsUnique();
            users.AddColumn(u => u.Password).HasLength(100).IsRequired();
            users.AddColumn(u => u.CreatedAt).IsTrackedTimestamp();
            yield return users.Generate(); // Генерация схемы, валидация и автоматический .Initialize()

            // 2. ⚡ СХЕМА ТАБЛИЦЫ СООБЩЕНИЙ (messages)
            var messages = new TableBuilder<MessageEntity>("Messages");
            messages.AddColumn(m => m.Id).AsPrimaryKey().AsAutoIncrement();
            messages.AddColumn(m => m.SenderId).IsRequired().HasForeignKey<UserEntity>();
            messages.AddColumn(m => m.ReceiverId).IsRequired().HasForeignKey<UserEntity>();
            messages.AddColumn(m => m.Text).HasLength(4000);
            messages.AddColumn(m => m.MediaType).HasLength(50).HasDefault("Text");
            messages.AddColumn(m => m.CloudUrl).AsText();
            messages.AddColumn(m => m.LocalPath).AsText();
            messages.AddColumn(m => m.IsRead).HasDefault(0);
            messages.AddColumn(m => m.CreatedAt).IsTrackedTimestamp();
            yield return messages.Generate();

            // 3. ⚡ СХЕМА ТАБЛИЦЫ КОНТАКТОВ / ДРУЗЕЙ (friends)
            var friends = new TableBuilder<FriendEntity>("Friends");
            // Так как у таблицы Friends составной ключ (UserId + FriendId), вешаем .AsPrimaryKey() на оба поля
            friends.AddColumn(f => f.UserId).IsRequired().HasForeignKey<UserEntity>();
            friends.AddColumn(f => f.FriendId).IsRequired().AsPrimaryKey().HasForeignKey<UserEntity>();
            friends.AddColumn(f => f.CreatedAt).IsTrackedTimestamp();
            yield return friends.Generate();
        }
    }
}
