using HomeNetCore.Data.Schemes;
using HomeNetCore.Models; // Твои C#-сущности (UserEntity, MessageEntity)
using HomeNetCoreTemperary.Data.Builders;
using System.Collections.Generic;
using WpfHomeNet.Data.Builders;

namespace HomeSocialNetwork.Core
{
    public static class SchemaRegistry
    {
        public static IEnumerable<TableSchema> GetAllSchemas()
        {
            // 1. СХЕМА ТАБЛИЦЫ ПОЛЬЗОВАТЕЛЕЙ (users)
            var usersTable = new TableSchema { TableName = "Users" };
            usersTable.Columns = new List<ColumnSchema>
            {
                new ColumnBuilder<UserEntity>(u => u.Id).AsPrimaryKey().AsAutoIncrement().Build(),
                new ColumnBuilder<UserEntity>(u => u.FirstName).HasLength(50).IsRequired().Build(),
                new ColumnBuilder<UserEntity>(u => u.LastName).HasLength(50).Build(),
                new ColumnBuilder<UserEntity>(u => u.PhoneNumber).HasLength(50).Build(),
                new ColumnBuilder<UserEntity>(u => u.Email).HasLength(50).IsRequired().IsUnique().Build(),
                new ColumnBuilder<UserEntity>(u => u.Password).HasLength(100).IsRequired().Build(),
                new ColumnBuilder<UserEntity>(u => u.CreatedAt).IsTrackedTimestamp().Build()
            };
            usersTable.Initialize();
            yield return usersTable;

            // 2. ⚡ НОВЫЙ ВАГОНЧИК: СХЕМА ТАБЛИЦЫ СООБЩЕНИЙ (messages)
            var messagesTable = new TableSchema { TableName = "Messages" };
            messagesTable.Columns = new List<ColumnSchema>
            {
                // ID сообщения
                new ColumnBuilder<MessageEntity>(m => m.Id).AsPrimaryKey().AsAutoIncrement().Build(),
                
                // Внешние ключи: связываем числовые поля с C#-классом UserEntity!
                new ColumnBuilder<MessageEntity>(m => m.SenderId).IsRequired().HasForeignKey<UserEntity>().Build(),
                new ColumnBuilder<MessageEntity>(m => m.ReceiverId).IsRequired().HasForeignKey<UserEntity>().Build(),
                
                // Текст чата (может быть null, если отправляем только картинку с Яндекс.Диска)
                new ColumnBuilder<MessageEntity>(m => m.Text).HasLength(4000).Build(),
                
                // Медиа-блок (тип: Text, Image, Video) и длинные строки путей/ссылок
                new ColumnBuilder<MessageEntity>(m => m.MediaType).HasLength(50).HasDefault("Text").Build(),
                new ColumnBuilder<MessageEntity>(m => m.CloudUrl).AsText().Build(),  // Тот самый Яндекс.Диск получателя
                new ColumnBuilder<MessageEntity>(m => m.LocalPath).AsText().Build(), // Локальный путь отправителя
                
                // Статус прочтения (0 или 1) и время
                new ColumnBuilder<MessageEntity>(m => m.IsRead).HasDefault(0).Build(),
                new ColumnBuilder<MessageEntity>(m => m.CreatedAt).IsTrackedTimestamp().Build()
            };
            messagesTable.Initialize();
            yield return messagesTable;
        }
    }
}
