using HomeNetCore.Models;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Builders;

namespace HomeNet.DI.Modules
{
    /// <summary>
    /// 🌐 УДАЛЕННЫЙ МОДУЛЬ СХЕМ (Для PostgreSQL): Общие таблицы с триггерами версий + составной ключ девайсов
    /// </summary>
    public class RemoteMessengerSchemaModule : BaseMessengerSchemaModule
    {
        public RemoteMessengerSchemaModule() : base()
        {
            // Добавляем таблицу синхронизации девайсов на составном ключе (Только на сервере!)
            var deviceSync = new TableBuilder<DeviceSyncEntity>("DeviceSync")
                .WithVersionTracking(); // Включаем серверный триггер для этой таблицы!

            deviceSync.AddColumn(d => d.DeviceId).AsPrimaryKey();
            deviceSync.AddColumn(d => d.UserId).AsPrimaryKey();
            deviceSync.AddColumn(d => d.Version); // Поле для триггера
            deviceSync.AddColumn(d => d.UpdatedAt);

            _schemas.Add(deviceSync.Generate());
        }

        // Внедряем серверное поле Version и включаем триггеры для общих таблиц
        protected override TableBuilder<UserEntity> BuildUsersSchema()
        {
            var builder = base.BuildUsersSchema().WithVersionTracking();
            builder.AddColumn(u => u.Version); // Твой ОРМ теперь знает про колонку Version
            return builder;
        }

        protected override TableBuilder<MessageEntity> BuildMessagesSchema()
        {
            var builder = base.BuildMessagesSchema().WithVersionTracking();
            builder.AddColumn(m => m.Version);
            return builder;
        }

        protected override TableBuilder<FriendEntity> BuildFriendsSchema()
        {
            var builder = base.BuildFriendsSchema().WithVersionTracking();
            builder.AddColumn(f => f.Version);
            return builder;
        }
    }
}

