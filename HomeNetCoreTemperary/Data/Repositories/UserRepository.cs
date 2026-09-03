using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;

namespace HomeNetCore.Data.Repositories
{
    // Наследуем базовый функционал CRUD, оставляем только уникальные методы
    public class UserRepository : BaseRepository<UserEntity, ISchemaUserSqlGenerator>, IUserRepository
    {
        public UserRepository(DbConnection connection, ISchemaUserSqlGenerator queryGenerator)
            : base(connection, queryGenerator) { }

        public async Task<bool> EmailExistsAsync(string? email)
        {
            var sql = _sqlGenerator.GenerateEmailExists();
            return await _connection.ExecuteScalarAsync<bool>(sql, new { email });
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            var sql = _sqlGenerator.GenerateSelectByEmail();
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { email = email });
        }

        // Перенаправляем старый метод InsertUserAsync на наш базовый универсальный InsertAsync
        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            return await InsertAsync(user);
        }
    }
}

