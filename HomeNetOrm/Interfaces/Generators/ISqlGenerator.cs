namespace HomeNetOrm.Interfaces.Generators
{
    
        public interface ISqlGenerator<TEntity> where TEntity : class
        {
            string GenerateInsert();
            string GenerateUpdate();
            string GenerateDelete(); // Удаление по сущности
            string GenerateSelectAll();
            string GenerateSelectById();
            object PrepareParameters(TEntity entity);
        }
    

}
