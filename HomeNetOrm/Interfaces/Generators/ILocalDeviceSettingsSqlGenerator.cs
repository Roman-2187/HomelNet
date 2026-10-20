namespace HomeNetOrm.Interfaces.Generators
{
    /// <summary>
    /// ⚙️ ЛОКАЛЬНЫЙ интерфейс генератора для настроек приложения на стороне клиента
    /// </summary>
    public interface ILocalDeviceSettingsSqlGenerator
    {
        string GenerateSelectByKey();
        string GenerateInsertOrUpdate();
    }
}
