using HomeNetCore.Models;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using HomeNetOrm.Models;

namespace HomeNetOrm.Schemes
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
            messages.AddColumn(m => m.Text).AsText();

            // Передаем дефолтное значение "Text", но тип принудительно ставим как у Text/Varchar
            messages.AddColumn(m => m.MediaType).AsText();

            // Передаем дефолт 0 и принудительно просим валидатор считать это поле INTEGER (числом)
            messages.AddColumn(m => m.IsRead).HasDefault(0, ColumnType.Integer);

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
