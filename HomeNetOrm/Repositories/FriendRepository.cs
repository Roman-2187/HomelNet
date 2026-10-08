using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Автономный репозиторий списка друзей/контактов SiberNet.
    /// Выполняет чистые атомарные CRUD-операции через инжектируемое подключение.
    /// </summary>
    public class FriendRepository : IFriendRepository
    {
        private readonly DbConnection _connection;
        private readonly ISqlGenerator<FriendEntity> _sqlGenerator;

        public FriendRepository(DbConnection connection, ISqlGenerator<FriendEntity> sqlGenerator)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        // 🤝 Добавить пользователя в друзья через ОРМ
        public async Task<bool> AddFriendAsync(FriendEntity friend)
        {
            if (friend.Id == Guid.Empty) friend.Id = Guid.NewGuid();
            friend.CreatedAt = DateTime.UtcNow;
            friend.UpdatedAt = DateTimeOffset.UtcNow;
            friend.IsSynced = 0; // Новая запись помечается грязной

            string sql = _sqlGenerator.GenerateInsert();

            try
            {
                // Так как мы зафиксировали поле .Id в схеме friends на прошлом шаге,
                // дженерик-генератор гладко выдаст инсерт с перехватом ID
                var newId = await _connection.ExecuteScalarAsync<Guid>(sql, friend);
                if (newId != Guid.Empty) friend.Id = newId;

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при добавлении в друзья через ОРМ: {ex.Message}");
            }
        }

        // ❌ Удалить связь из таблицы friends по GUID через ОРМ
        public async Task<bool> RemoveFriendByIdAsync(Guid id)
        {
            string sql = _sqlGenerator.GenerateDelete();

            try
            {
                int rowsAffected = await _connection.ExecuteAsync(sql, new { id = id });
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка удаления связи контактов через ОРМ: {ex.Message}");
            }
        }

        // 👥 Вытащить профили всех друзей (ANSI SQL, полностью независимый от СУБД)
        public async Task<IEnumerable<UserEntity>> GetFriendsForUserAsync(Guid userId)
        {
            const string sql = @"SELECT u.* FROM users u
                                 INNER JOIN friends f ON u.id = f.friend_id 
                                 WHERE f.user_id = @UserId;";
            try
            {
                return await _connection.QueryAsync<UserEntity>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка выборки друзей из базы данных: {ex.Message}");
            }
        }
    }
}
