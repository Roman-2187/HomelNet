using Dapper;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using HomeNetCore.Exeptions;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Репозиторий пользователей SiberNet. 
    /// Пишет одновременно в SQLite и Postgres, обеспечивая мгновенную отказоустойчивость.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly DbContextContainer _context;

        public UserRepository(DbContextContainer context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            // 1. Готовим идентификаторы и метки времени на клиенте
            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
            }
            user.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                // 2. Локальный SQLite — пишем ВСЕГДА, это наш главный оплот
                var sqliteSql = _context.SqliteUserSqlGen.GenerateInsert();

                // Если при вставке в офлайне сеть лежит, ставим флаг синхронизации в 0
                user.IsSynced = _context.IsPostgresAvailable ? 1 : 0;
                await _context.SqliteConnection.ExecuteAsync(sqliteSql, user);

                // 3. Центральный Postgres — пишем параллельно, если он в сети
                if (_context.IsPostgresAvailable)
                {
                    try
                    {
                        var pgSql = _context.PostgresUserSqlGen.GenerateInsert();
                        await _context.PostgresConnection.ExecuteAsync(pgSql, user);
                    }
                    catch (Exception pgEx)
                    {
                        // Если Постгрес неожиданно упал в процессе — не ломаем приложение!
                        // Мягко помечаем локальную запись как неотправленную
                        await _context.SqliteConnection.ExecuteAsync(
                            "UPDATE users SET is_synced = 0 WHERE id = @Id", new { Id = user.Id });
                        System.Diagnostics.Debug.WriteLine($"[Repo] Postgres отвалился при вставке: {pgEx.Message}");
                    }
                }

                return user;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"Ошибка при вставке пользователя: {ex.Message}");
            }
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            try
            {
                // 1. Сначала удаляем из локального SQLite
                var sqliteSql = _context.SqliteUserSqlGen.GenerateDelete();
                var affectedRows = await _context.SqliteConnection.ExecuteAsync(sqliteSql, new { id = id });

                if (affectedRows == 0)
                {
                    throw new NotFoundException($"Пользователь с ID {id} не найден в локальной БД.");
                }

                // 2. Если Postgres доступен — гасим запись и там (в Постгресе отработает наш фикс ::text)
                if (_context.IsPostgresAvailable)
                {
                    try
                    {
                        var pgSql = _context.PostgresUserSqlGen.GenerateDelete();
                        await _context.PostgresConnection.ExecuteAsync(pgSql, new { id = id });
                    }
                    catch (Exception pgEx)
                    {
                        // Если сеть моргнула — не падаем. Синхронизатор при следующем старте подчистит хвосты
                        System.Diagnostics.Debug.WriteLine($"[Repo] Ошибка удаления из Postgres: {pgEx.Message}");
                    }
                }
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                throw new InvalidOperationException($"Ошибка при удалении: {ex.Message}");
            }
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
            // Читаем ВСЕГДА из локального SQLite — это мгновенно, без лагов сети и тормозов UI!
            string sql = _context.SqliteUserSqlGen.GenerateSelectAll();

            try
            {
                var users = (await _context.SqliteConnection.QueryAsync<UserEntity>(sql)).ToList();
                return users ?? throw new InvalidOperationException("Не удалось получить данные из SQLite");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка получения списка пользователей: {ex.Message}");
            }
        }

        public async Task<UserEntity?> GetByIdAsync(Guid id)
        {
            // Читаем из локального SQLite
            string sql = _context.SqliteUserSqlGen.GenerateSelectById();
            return await _context.SqliteConnection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { id = id });
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            // Читаем из локального SQLite
            string sql = _context.SqliteUserSqlGen.GenerateSelectByEmail();
            return await _context.SqliteConnection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { email = email });
        }

        public async Task UpdateAsync(UserEntity user)
        {
            user.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                // 1. Апдейтим SQLite
                string sqliteSql = _context.SqliteUserSqlGen.GenerateUpdate();
                user.IsSynced = _context.IsPostgresAvailable ? 1 : 0;
                await _context.SqliteConnection.ExecuteAsync(sqliteSql, user);

                // 2. Апдейтим Postgres
                if (_context.IsPostgresAvailable)
                {
                    try
                    {
                        string pgSql = _context.PostgresUserSqlGen.GenerateUpdate();
                        await _context.PostgresConnection.ExecuteAsync(pgSql, user);
                    }
                    catch (Exception pgEx)
                    {
                        await _context.SqliteConnection.ExecuteAsync(
                            "UPDATE users SET is_synced = 0 WHERE id = @Id", new { Id = user.Id });
                        System.Diagnostics.Debug.WriteLine($"[Repo] Ошибка апдейта в Postgres: {pgEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка обновления пользователя: {ex.Message}");
            }
        }

        public async Task<bool> EmailExistsAsync(string? email)
        {
            // Проверка уникальности Email идет строго по локальной базе — это гарантирует моментальный отклик UI
            var sql = _context.SqliteUserSqlGen.GenerateEmailExists();
            return await _context.SqliteConnection.ExecuteScalarAsync<bool>(sql, new { email });
        }
    }
}
