using System.Data.Common;
using Dapper;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces;
namespace HomeNetOrm.Repositories
{
    // Подключаем контракт IFriendRepository! 🔌
    public class FriendRepository : IFriendRepository
    {
        private readonly DbConnection _connection;
        private readonly ISqlGenerator<FriendEntity> _sqlGen;

        public FriendRepository(DbConnection connection, ISqlGenerator<FriendEntity> sqlGen)
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

        // ❌ Удалить связь/заявку из таблицы friends по её уникальному ID
        public async Task<bool> RemoveFriendByIdAsync(int id)
        {
            // Дженерик-генератор сам соберёт DELETE FROM friends WHERE id = @id под нужную СУБД! 🚀
            string sql = _sqlGen.GenerateDelete();
            int rowsAffected = await _connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        // 👥 Вытащить профили всех юзеров, которые находятся в друзьях у конкретного человека
        public async Task<IEnumerable<UserEntity>> GetFriendsForUserAsync(int userId)
        {
            string sql = @"SELECT u.* FROM users u
                           INNER JOIN friends f ON u.id = f.friend_id 
                           WHERE f.user_id = @UserId;";

            return await _connection.QueryAsync<UserEntity>(sql, new { UserId = userId });
        }
    }
}
