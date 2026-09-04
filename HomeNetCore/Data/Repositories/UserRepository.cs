using Dapper;
using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.DBProviders.Sqlite.HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace HomeNetCore.Data.Repositories
{
    // 🔥 Теперь репозиторий гордо наследует дженерик-интерфейс и принимает SqliteSqlGenerator<UserEntity>!
    public class UserRepository : IUserRepository
    {
        private readonly DbConnection _connection;
        private readonly SqliteSqlGenerator<UserEntity> _userSqlGenerator;

        // БЫЛО: public UserRepository(DbConnection connection, ISqliteSqlGenerator<UserEntity> generator)
        // СТАЛО:
        // БЫЛО: public UserRepository(DbConnection connection, ISqLiteSqlGenerator<UserEntity> generator)
        // СТАЛО:
        public UserRepository(DbConnection connection, SqliteSqlGenerator<UserEntity> generator)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _userSqlGenerator = generator ?? throw new ArgumentNullException(nameof(generator));
        }


        public async Task<bool> EmailExistsAsync(string? email)
        {
            var sql = _userSqlGenerator.GenerateEmailExists();
            return await _connection.ExecuteScalarAsync<bool>(sql, new { email });
        }

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            try
            {
                var sql = _userSqlGenerator.GenerateInsert();
                var newId = await _connection.ExecuteScalarAsync<int>(sql, user);
                user.Id = newId;
                return user;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"Ошибка при вставке: {ex.Message}");
            }
        }

        public async Task DeleteByIdAsync(int id)
        {
            var affectedRows = await _connection.ExecuteAsync(_userSqlGenerator.GenerateDelete(), new { id = id });

            if (affectedRows == 0)
            {
                throw new NotFoundException($"Пользователь с ID {id} не найден.");
            }
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
            string sql = _userSqlGenerator.GenerateSelectAll();

            try
            {
                // Выполняем запрос через Dapper
                var users = (await _connection.QueryAsync<UserEntity>(sql)).ToList();

                // Проверяем результат
                return users ?? throw new InvalidOperationException("Не удалось получить данные из БД");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось выполнить запрос получения пользователей: {ex.Message}");
            }
        }

        public async Task<UserEntity?> GetByIdAsync(int id)
        {
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(_userSqlGenerator.GenerateSelectById(), new { id = id });
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(_userSqlGenerator.GenerateSelectByEmail(), new { email = email });
        }

        public async Task UpdateAsync(UserEntity user)
        {
            await _connection.ExecuteAsync(_userSqlGenerator.GenerateUpdate(), user);
        }
    }
}
