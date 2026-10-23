using HomeNetCore.Models.Entity;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;

namespace HomeNet.DI.Modules
{
    /// <summary>
    /// 📱 ЛОКАЛЬНЫЙ МОДУЛЬ СХЕМ (Для SQLite): Общие таблицы + флаги синхронизации + сессия автовхода
    /// </summary>
    public class LocalMessengerSchemaModule : BaseMessengerSchemaModule
    {
        public LocalMessengerSchemaModule() : base()
        {
            // Добавляем таблицу настроек сессии автовхода (В LocalMessengerSchemaModule.cs)
            var deviceSettings = new TableBuilder<DeviceSettingsEntity>("DeviceSettings");
            deviceSettings.AddColumn(s => s.DeviceId).AsPrimaryKey();
           

            // 🔥 УБРАЛИ .IsRequired()! Теперь SQLite разрешит этим полям быть пустыми при первом старте девайса
            deviceSettings.AddColumn(s => s.UserId);
            deviceSettings.AddColumn(s => s.Email);
            deviceSettings.AddColumn(s => s.Password);

            deviceSettings.AddColumn(s => s.UpdatedAt);

            _schemas.Add(deviceSettings.Generate());

        }

        protected override TableBuilder<UserEntity> BuildUsersSchema()
        {
            var builder = base.BuildUsersSchema();
            // 🎯 Меняем 1 на 0, чтобы совпадало с реальной базой
            builder.AddColumn(u => u.IsSynced).HasDefault(0, ColumnType.Integer);
            return builder;
        }

        protected override TableBuilder<MessageEntity> BuildMessagesSchema()
        {
            var builder = base.BuildMessagesSchema();
            builder.AddColumn(m => m.IsSynced).HasDefault(0, ColumnType.Integer);
            return builder;
        }

        protected override TableBuilder<FriendEntity> BuildFriendsSchema()
        {
            var builder = base.BuildFriendsSchema();
            // 🎯 Меняем 1 на 0
            builder.AddColumn(f => f.IsSynced).HasDefault(0, ColumnType.Integer);
            return builder;
        }


    }
}
