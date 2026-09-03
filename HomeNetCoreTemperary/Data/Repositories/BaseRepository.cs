using Dapper;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Helpers.Exceptions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNetCore.Data.Repositories
{
    // Универсальная база для абсолютно любого репозитория (Users, Messages, Friends)
    public abstract class BaseRepository<TEntity, TGenerator>
        where TEntity : class
        where TGenerator : ISchemaSqlGenerator<TEntity>
    {
        protected readonly DbConnection _connection;
        protected readonly TGenerator _sqlGenerator;

        protected BaseRepository(DbConnection connection, TGenerator queryGenerator)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = queryGenerator ?? throw new ArgumentNullException(nameof(queryGenerator));
        }

        public virtual async Task<TEntity> InsertAsync(TEntity entity)
        {
            try
            {
                var sql = _sqlGenerator.GenerateInsert();
                var newId = await _connection.ExecuteScalarAsync<int>(sql, entity);

                // Используем рефлексию, чтобы мягко проставить ID, если свойство существует
                var idProperty = typeof(TEntity).GetProperty("Id");
                if (idProperty != null && idProperty.CanWrite)
                {
                    idProperty.SetValue(entity, Convert.ChangeType(newId, idProperty.PropertyType));
                }

                return entity;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"Ошибка при вставке в {typeof(TEntity).Name}: {ex.Message}");
            }
        }

        public virtual async Task DeleteByIdAsync(int id)
        {
            var sql = _sqlGenerator.GenerateDelete();
            // Получаем имя ID колонки динамически из параметров адаптера, если нужно, 
            // но так как Dapper маппит на анонимный объект, передаем стандартный id
            var affectedRows = await _connection.ExecuteAsync(sql, new { id = id });

            if (affectedRows == 0)
            {
                throw new NotFoundException($"Запись в {typeof(TEntity).Name} с ID {id} не найдена.");
            }
        }

        public virtual async Task<List<TEntity>> GetAllAsync()
        {
            string sql = _sqlGenerator.GenerateSelectAll();
            try
            {
                var result = (await _connection.QueryAsync<TEntity>(sql)).ToList();
                return result ?? throw new InvalidOperationException($"Не удалось получить данные {typeof(TEntity).Name} из БД");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось выполнить запрос получения {typeof(TEntity).Name}: {ex.Message}");
            }
        }

        public virtual async Task<TEntity?> GetByIdAsync(int id)
        {
            string sql = _sqlGenerator.GenerateSelectById();
            return await _connection.QueryFirstOrDefaultAsync<TEntity>(sql, new { id = id });
        }

        public virtual async Task UpdateAsync(TEntity entity)
        {
            string sql = _sqlGenerator.GenerateUpdate();
            await _connection.ExecuteAsync(sql, entity);
        }
    }
}
