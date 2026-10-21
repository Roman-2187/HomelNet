using HomeNetOrm.Builders;
using HomeNetCore.Models;

namespace HomeNet.DI.Modules
{
    /// <summary>
    /// 🌐 УДАЛЕННЫЙ МОДУЛЬ СХЕМ (Для PostgreSQL): Общие таблицы + метки синхронизации
    /// </summary>
    public class RemoteMessengerSchemaModule : BaseMessengerSchemaModule
    {
        public RemoteMessengerSchemaModule() : base()
        {
            // Добавляем специфичную таблицу, которая нужна ТОЛЬКО на сервере
            var deviceSync = new TableBuilder<DeviceSyncEntity>("DeviceSync");
            deviceSync.AddColumn(d => d.Id).AsPrimaryKey();
            deviceSync.AddColumn(d => d.DeviceId).IsRequired();
            deviceSync.AddColumn(d => d.UserId).IsRequired().HasForeignKey<UserEntity>();
            deviceSync.AddColumn(d => d.LastSyncedAt).IsTrackedTimestamp();
            deviceSync.AddColumn(d => d.UpdatedAt).IsUpdatedAtTimestamp();

            _schemas.Add(deviceSync.Generate());
        }




    }
}