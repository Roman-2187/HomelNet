using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.DBProviders.Sqlite.HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;

namespace HomeNetCore.Data.Repositories
{
    public class FriendRepository
    {
        private readonly DbConnection _connection;
        private readonly ISqLiteSqlGenerator<FriendEntity> _sqlGen;

        public FriendRepository(DbConnection connection, ISqLiteSqlGenerator<FriendEntity> sqlGen)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGen = sqlGen ?? throw new ArgumentNullException(nameof(sqlGen));
        }

        // 🤝 Добавить родственника в список контактов
        public async Task<bool> AddFriendAsync(FriendEntity friend)
        {
            string sql = _sqlGen.GenerateInsert();
            int rowsAffected = await _connection.ExecuteAsync(sql, friend);
            return rowsAffected > 0;
        }

        // 👥 Вытащить профили всех юзеров, которые находятся в друзьях у конкретного человека
        public async Task<IEnumerable<UserEntity>> GetFriendsForUserAsync(int userId)
        {
            // По ID друга из связующей таблицы friends лезем в таблицу users за именами!
            string sql = @"SELECT u.* FROM users u
                           INNER JOIN friends f ON u.id = f.friend_id 
                           WHERE f.user_id = @UserId;";

            return await _connection.QueryAsync<UserEntity>(sql, new { UserId = userId });
        }
    }
}

