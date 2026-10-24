using System;
using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{
    /// <summary>
    /// Интерфейс генератора синхронизации устройств на сервере.
    /// Полностью переведен на Command Object. 🦾
    /// </summary>
    internal interface IRemoteDeviceSyncSqlGenerator
    {
        SqlQueryCommand GenerateDeviceSyncUpsert();
        SqlQueryCommand GenerateSelectUpdatesByTimestamp(DateTimeOffset timestamp);
        SqlQueryCommand GenerateSelectUserIdByDevice(Guid deviceId);
    }
}
