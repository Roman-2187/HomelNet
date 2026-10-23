using HomeNetCore.Models.Entity;
using HomeNetOrm.Builders;
using HomeNetOrm.Models;

namespace HomeNet.DI.Modules
{
    /// <summary>
    /// 🏗️ БАЗОВЫЙ МОДУЛЬ СХЕМ: Содержит только общие поля для обеих СУБД
    /// </summary>
    public abstract class BaseMessengerSchemaModule
    {
        protected readonly List<TableSchema> _schemas = new();

        protected BaseMessengerSchemaModule()
        {
            BuildCommonSchemas();
        }

        private void BuildCommonSchemas()
        {
            // 1. СХЕМА ТАБЛИЦЫ ПОЛЬЗОВАТЕЛЕЙ (Users)
            var users = BuildUsersSchema();
            _schemas.Add(users.Generate());

            // 2. СХЕМА ТАБЛИЦЫ СООБЩЕНИЙ (Messages)
            var messages = BuildMessagesSchema();
            _schemas.Add(messages.Generate());

            // 3. СХЕМА ТАБЛИЦЫ КОНТАКТОВ / ДРУЗЕЙ (Friends)
            var friends = BuildFriendsSchema();
            _schemas.Add(friends.Generate());
        }

        // Выносим в защищенные методы, чтобы наследники (Local/Remote) могли докинуть свои поля!
        protected virtual TableBuilder<UserEntity> BuildUsersSchema()
        {
            var builder = new TableBuilder<UserEntity>("Users");
            builder.AddColumn(u => u.Id).AsPrimaryKey();
            builder.AddColumn(u => u.DeviceId).IsRequired(); // Наш сетевой связующий хвост!
            builder.AddColumn(u => u.FirstName).HasLength(50).IsRequired();
            builder.AddColumn(u => u.LastName).HasLength(50);
            builder.AddColumn(u => u.PhoneNumber).HasLength(50);
            builder.AddColumn(u => u.Email).HasLength(50).IsRequired();
            builder.AddColumn(u => u.Password).HasLength(100).IsRequired();
            builder.AddColumn(u => u.CreatedAt);
            builder.AddColumn(u => u.UpdatedAt);
            return builder;
        }

        protected virtual TableBuilder<MessageEntity> BuildMessagesSchema()
        {
            var builder = new TableBuilder<MessageEntity>("Messages");
            builder.AddColumn(m => m.Id).AsPrimaryKey();
            builder.AddColumn(m => m.SenderId).IsRequired();
            builder.AddColumn(m => m.ReceiverId).IsRequired();
            builder.AddColumn(m => m.Text).AsText();
            builder.AddColumn(m => m.MediaType).AsText();
            builder.AddColumn(m => m.IsRead);
            builder.AddColumn(m => m.CreatedAt);
            builder.AddColumn(m => m.UpdatedAt);
            return builder;
        }

        protected virtual TableBuilder<FriendEntity> BuildFriendsSchema()
        {
            var builder = new TableBuilder<FriendEntity>("Friends");
            builder.AddColumn(f => f.Id).AsPrimaryKey();
            builder.AddColumn(f => f.UserId).IsRequired();
            builder.AddColumn(f => f.FriendId).IsRequired();
            builder.AddColumn(f => f.CreatedAt);
            builder.AddColumn(f => f.UpdatedAt);
            return builder;
        }

        public IEnumerable<TableSchema> GetSchemas() => _schemas;
    }
}
