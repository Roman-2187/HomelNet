using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{
    /// <summary>
    /// ⚙️ ЛОКАЛЬНЫЙ интерфейс генератора для настроек приложения на стороне клиента.
    /// Полностью переведен на Command Object под вызовы через точку! 🦾
    /// </summary>
   internal interface ILocalDeviceSettingsSqlGenerator
    {
        // 🎯 ФИКС CONTRACT: Теперь генератор настроек штампует готовые команды запросов
        SqlQueryCommand GenerateSelectByKey();
        SqlQueryCommand GenerateInsertOrUpdate();
    }
}
