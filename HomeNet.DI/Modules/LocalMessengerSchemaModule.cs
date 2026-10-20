using HomeNetOrm.Builders;
using HomeNetCore.Models;

namespace HomeNet.DI.Modules
{
    /// <summary>
    /// 📱 ЛОКАЛЬНЫЙ МОДУЛЬ СХЕМ (Для SQLite): Общие таблицы + настройки девайса
    /// </summary>
    public class LocalMessengerSchemaModule : BaseMessengerSchemaModule
    {
        public LocalMessengerSchemaModule() : base()
        {
            // Добавляем специфичную таблицу, которая нужна ТОЛЬКО на клиенте
            var deviceSettings = new TableBuilder<DeviceSyncEntity>("DeviceSettings");
            deviceSettings.AddColumn(s => s.DeviceId).AsPrimaryKey();
            deviceSettings.AddColumn(s => s.Id).AsText().IsRequired();

            _schemas.Add(deviceSettings.Generate());
        }
    }
}
