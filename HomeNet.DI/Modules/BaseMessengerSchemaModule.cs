using System.Collections.Generic;
using HomeNetOrm.Builders;
using HomeNetOrm.Models;
using HomeNetOrm.Enums;
using HomeNetCore.Models;

namespace HomeNet.DI.Modules
{
    /// <summary>
    /// 🏗️ БАЗОВЫЙ МОДУЛЬ СХЕМ: Содержит только общие таблицы для обеих СУБД
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
            // 1. СХЕМА ТАБЛИЦЫ ПОЛЬЗОВАТЕЛЕЙ (users)
            var users = new TableBuilder<UserEntity>("Users");
            users.AddColumn(u => u.Id).AsPrimaryKey();
            users.AddColumn(u => u.FirstName).HasLength(50).IsRequired();
            users.AddColumn(u => u.LastName).HasLength(50);
            users.AddColumn(u => u.PhoneNumber).HasLength(50);
            users.AddColumn(u => u.Email).HasLength(50).IsRequired().IsUnique();
            users.AddColumn(u => u.Password).HasLength(100).IsRequired();
            users.AddColumn(u => u.CreatedAt).IsTrackedTimestamp();
            users.AddColumn(u => u.IsSynced).HasDefault(1, ColumnType.Integer);
            users.AddColumn(u => u.UpdatedAt).IsUpdatedAtTimestamp();
            _schemas.Add(users.Generate());

            // 2. ⚡ СХЕМА ТАБЛИЦЫ СООБЩЕНИЙ (messages)
            var messages = new TableBuilder<MessageEntity>("Messages");
            messages.AddColumn(m => m.Id).AsPrimaryKey();
            messages.AddColumn(m => m.SenderId).IsRequired().HasForeignKey<UserEntity>();
            messages.AddColumn(m => m.ReceiverId).IsRequired().HasForeignKey<UserEntity>();
            messages.AddColumn(m => m.Text).AsText();
            messages.AddColumn(m => m.MediaType).AsText();
            messages.AddColumn(m => m.IsRead).HasDefault(false, ColumnType.Boolean);
            messages.AddColumn(m => m.CreatedAt).IsTrackedTimestamp();
            messages.AddColumn(m => m.IsSynced).HasDefault(false, ColumnType.Boolean);
            messages.AddColumn(m => m.UpdatedAt).IsUpdatedAtTimestamp();
            _schemas.Add(messages.Generate());

            // 3. ⚡ СХЕМА ТАБЛИЦЫ КОНТАКТОВ / ДРУЗЕЙ (friends)
            var friends = new TableBuilder<FriendEntity>("Friends");
            friends.AddColumn(f => f.Id).AsPrimaryKey();
            friends.AddColumn(f => f.UserId).IsRequired().HasForeignKey<UserEntity>();
            friends.AddColumn(f => f.FriendId).IsRequired().HasForeignKey<UserEntity>();
            friends.AddColumn(f => f.CreatedAt).IsTrackedTimestamp();
            friends.AddColumn(f => f.IsSynced).HasDefault(1, ColumnType.Integer);
            friends.AddColumn(f => f.UpdatedAt).IsUpdatedAtTimestamp();
            _schemas.Add(friends.Generate());
        }

        public IEnumerable<TableSchema> GetSchemas() => _schemas;
    }
}