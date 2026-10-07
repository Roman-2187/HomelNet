using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Builders;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Репозиторий списка друзей/контактов SiberNet.
    /// Работает параллельно с локальным SQLite и центральным PostgreSQL.
    /// </summary>
    public class FriendRepository : IFriendRepository
    {
        private readonly DbContextContainer _context;

        public FriendRepository(DbContextContainer context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // 🤝 Добавить пользователя в друзья сразу в обе базы данных
        public async Task<bool> AddFriendAsync(FriendEntity friend)
        {
            // Штампуем Guid ключи и метки времени изменения на клиенте
            if (friend.Id == Guid.Empty) friend.Id = Guid.NewGuid();
            friend.CreatedAt = DateTime.UtcNow;
            friend.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                // 1. Локальный SQLite — пишем всегда, это наш главный оплот
                string sqliteSql = _context.SqliteFriendSqlGen.GenerateInsert();

                // Ставим флаг синхронизации: 1 если Postgres онлайн, 0 если офлайн
                friend.IsSynced = _context.IsPostgresAvailable ? 1 : 0;
                int sqliteRows = await _context.SqliteConnection.ExecuteAsync(sqliteSql, friend);

                // 2. Удаленный PostgreSQL — пишем параллельно, если есть связь
                if (_context.IsPostgresAvailable)
                {
                    try
                    {
                        string pgSql = _context.PostgresFriendSqlGen.GenerateInsert();
                        await _context.PostgresConnection.ExecuteAsync(pgSql, friend);
                    }
                    catch (Exception pgEx)
                    {
                        // При обрыве связи мягко помечаем локальную запись как неотправленную
                        await _context.SqliteConnection.ExecuteAsync(
                            "UPDATE friends SET is_synced = 0 WHERE id = @Id", new { Id = friend.Id });
                        System.Diagnostics.Debug.WriteLine($"[FriendRepo] Сбой дублирования в Postgres: {pgEx.Message}");
                    }
                }

                return sqliteRows > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при добавлении в друзья: {ex.Message}");
            }
        }

        // ❌ Удалить связь из таблицы friends по её уникальному GUID в обеих базах
        public async Task<bool> RemoveFriendByIdAsync(Guid id)
        {
            try
            {
                // 1. Удаляем из SQLite
                string sqliteSql = _context.SqliteFriendSqlGen.GenerateDelete();
                int rowsAffected = await _context.SqliteConnection.ExecuteAsync(sqliteSql, new { Id = id });

                // 2. Удаляем из Postgres
                if (_context.IsPostgresAvailable)
                {
                    try
                    {
                        string pgSql = _context.PostgresFriendSqlGen.GenerateDelete();
                        await _context.PostgresConnection.ExecuteAsync(pgSql, new { Id = id });
                    }
                    catch (Exception pgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FriendRepo] Ошибка физического удаления в Postgres: {pgEx.Message}");
                    }
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при удалении связи контактов: {ex.Message}");
            }
        }

        // 👥 Вытащить профили всех юзеров, которые находятся в друзьях у конкретного человека
        public async Task<IEnumerable<UserEntity>> GetFriendsForUserAsync(Guid userId)
        {
            // Выборку контактов делаем всегда из быстрого локального SQLite без лагов сети
            string sql = @"SELECT u.* FROM users u
                           INNER JOIN friends f ON u.id = f.friend_id 
                           WHERE f.user_id = @UserId;";
            try
            {
                return await _context.SqliteConnection.QueryAsync<UserEntity>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка выборки друзей из локального SQLite: {ex.Message}");
            }
        }
    }
}
