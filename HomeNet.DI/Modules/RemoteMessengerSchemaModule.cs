using HomeNet.Models.Shared.Entity;
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
            // Серверная таблица девайсов на составном ключе
            var deviceSync = new TableBuilder<DeviceSyncEntity>("DeviceSync")
                .WithVersionTracking();

            deviceSync.AddColumn(d => d.DeviceId).AsPrimaryKey();
            deviceSync.AddColumn(d => d.UserId).AsPrimaryKey();
            deviceSync.AddColumn(d => d.Version);

            // 🔥 ПРИВЕЛИ ДАТЫ В ПОРЯДОК: У девайсов нет Local/Server логики, они пишут техническое время
            deviceSync.AddColumn(d => d.CreatedAt);
            deviceSync.AddColumn(d => d.UpdatedAt);

            _schemas.Add(deviceSync.Generate());
        }

        protected override TableBuilder<UserEntity> BuildUsersSchema()
        {
            var builder = base.BuildUsersSchema().WithVersionTracking();
            
            builder.AddColumn(f => f.ServerCreatedAt);
            builder.AddColumn(f => f.UpdatedAt);
            builder.AddColumn(u => u.Version);
            return builder;
        }

        protected override TableBuilder<MessageEntity> BuildMessagesSchema()
        {
            var builder = base.BuildMessagesSchema().WithVersionTracking();
           
            builder.AddColumn(m => m.ServerCreatedAt);
            builder.AddColumn(m => m.UpdatedAt);
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
