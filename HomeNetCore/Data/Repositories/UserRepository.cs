using Dapper;
using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Helpers.Exceptions;
using HomeNetCore.Models;
using System.Data.Common;

namespace HomeNetCore.Data.Repositories
{
    // 🔥 Теперь репозиторий гордо наследует дженерик-интерфейс и принимает SqliteSqlGenerator<UserEntity>!
    public class UserRepository : IUserRepository
    {
        private readonly DbConnection _connection;
        // БЫЛО: private readonly SqliteSqlGenerator<UserEntity> _sqlGen;
        // СТАЛО:
        private readonly ISqlGenerator<UserEntity> _sqlGen;

        // В конструкторе меняем тип параметра:
        public UserRepository(DbConnection connection, ISqlGenerator<UserEntity> sqlGen)
        {
            _connection = connection;
            _sqlGen = sqlGen;
        }



        public async Task<bool> EmailExistsAsync(string? email)
        {
            var sql = _sqlGen.GenerateEmailExists();
            return await _connection.ExecuteScalarAsync<bool>(sql, new { email });
        }

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            try
            {
                var sql = _sqlGen.GenerateInsert();
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
            var affectedRows = await _connection.ExecuteAsync(_sqlGen.GenerateDelete(), new { id = id });

            if (affectedRows == 0)
            {
                throw new NotFoundException($"Пользователь с ID {id} не найден.");
            }
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
            string sql = _sqlGen.GenerateSelectAll();

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
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(_sqlGen.GenerateSelectById(), new { id = id });
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(_sqlGen.GenerateSelectByEmail(), new { email = email });
        }

        public async Task UpdateAsync(UserEntity user)
        {
            await _connection.ExecuteAsync(_sqlGen.GenerateUpdate(), user);
        }
    }
}
