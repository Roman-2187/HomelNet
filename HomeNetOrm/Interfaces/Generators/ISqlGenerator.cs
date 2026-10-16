namespace HomeNetOrm.Interfaces.Generators
{
    public interface ISqlGenerator<TEntity> where TEntity : class
    {
        string GenerateInsert();
        string GenerateUpdate();
        string GenerateDelete();
        string GenerateSelectAll();
        string GenerateSelectById();

        // 🎯 ДОБАВЛЯЕМ СЮДА КОНТРАКТЫ ДЛЯ СИНХРОНИЗАЦИИ:
        string GenerateSelectUnsynced();
        string GenerateUpdateMarkAsSynced();
        string GenerateCountById();
        string GenerateDeleteById();

        object PrepareParameters(TEntity entity);
    }
}
