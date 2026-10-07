using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Репозиторий сообщений SiberNet.
    /// Пишет одновременно в SQLite и Postgres, обеспечивая мгновенную отказоустойчивость чата.
    /// </summary>
    public class MessageRepository : IMessageRepository
    {
        private readonly DbContextContainer _context;

        public MessageRepository(DbContextContainer context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // 💬 Сохранить СМС в базу на автопилоте дженерика!
        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            // Готовим штамп времени на клиенте
            message.CreatedAt = DateTime.UtcNow;

            try
            {
                // 1. В локальный SQLite пишем ВСЕГДА
                string sqliteSql = _context.SqliteMessageSqlGen.GenerateInsert();

                // Ставим флаг синхронизации: 1 если Postgres онлайн, 0 если офлайн
                message.IsSynced = _context.IsPostgresAvailable ? 1 : 0;
                int sqliteRows = await _context.SqliteConnection.ExecuteAsync(sqliteSql, message);

                // 2. В центральный Postgres пишем параллельно, если он доступен
                if (_context.IsPostgresAvailable)
                {
                    try
                    {
                        string pgSql = _context.PostgresMessageSqlGen.GenerateInsert();
                        await _context.PostgresConnection.ExecuteAsync(pgSql, message);
                    }
                    catch (Exception pgEx)
                    {
                        // Если сеть моргнула во время отправки — не крашим чат!
                        // Мягко помечаем локальную запись в SQLite как неотправленную
                        await _context.SqliteConnection.ExecuteAsync(
                            "UPDATE messages SET is_synced = 0 WHERE id = @Id", new { Id = message.Id });
                        System.Diagnostics.Debug.WriteLine($"[MsgRepo] Ошибка дублирования в Postgres: {pgEx.Message}");
                    }
                }

                return sqliteRows > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка сохранения сообщения: {ex.Message}");
            }
        }

        // 🔍 Выгрузить историю переписки конкретной пары (Вася + Иван) БЕЗ ЛАГОВ СЕТИ
        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            try
            {
                // История чата ВСЕГДА мгновенно вычитывается из локального SQLite! 
                // Никаких зависаний UI и сетевых прыжков на лету.
                string sql = _context.SqliteMessageSqlGen.GenerateSelectChatHistory();
                return await _context.SqliteConnection.QueryAsync<MessageEntity>(sql, new { userId = senderId, friendId = receiverId });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось получить историю чата из SQLite: {ex.Message}");
            }
        }

        // 🧼 Отметить сообщения диалога как прочитанные в обеих базах
        public async Task<bool> MarkAsReadAsync(int senderId, int receiverId)
        {
            string sql = @"UPDATE messages SET is_read = 1 
                           WHERE sender_id = @SenderId AND receiver_id = @ReceiverId AND is_read = 0;";

            // 1. Локальный апдейт
            int rowsAffected = await _context.SqliteConnection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });

            // 2. Удаленный апдейт
            if (_context.IsPostgresAvailable)
            {
                try
                {
                    await _context.PostgresConnection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });
                }
                catch (Exception pgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MsgRepo] Не удалось обновить статус прочитано в Postgres: {pgEx.Message}");
                }
            }

            return rowsAffected > 0;
        }

        // 🔢 Считаем unread-статус строго по локальной базе для мгновенного отклика
        public async Task<int> GetUnreadCountAsync(int currentUserId, int senderId)
        {
            string sql = @"SELECT COUNT(*) FROM messages 
                           WHERE receiver_id = @CurrentUserId 
                             AND sender_id = @SenderId 
                             AND is_read = 0;";

            return await _context.SqliteConnection.ExecuteScalarAsync<int>(sql, new { CurrentUserId = currentUserId, SenderId = senderId });
        }

        // ❌ Полное физическое удаление конкретного сообщения по его ID (Удалить у всех)
        public async Task<bool> RemoveMessageByIdAsync(int id)
        {
            // 1. Удаляем из SQLite
            string sqliteSql = _context.SqliteMessageSqlGen.GenerateDelete();
            int rowsAffected = await _context.SqliteConnection.ExecuteAsync(sqliteSql, new { Id = id });

            // 2. Удаляем из Postgres
            if (_context.IsPostgresAvailable)
            {
                try
                {
                    string pgSql = _context.PostgresMessageSqlGen.GenerateDelete();
                    await _context.PostgresConnection.ExecuteAsync(pgSql, new { Id = id });
                }
                catch (Exception pgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MsgRepo] Ошибка физического удаления в Postgres: {pgEx.Message}");
                }
            }

            return rowsAffected > 0;
        }

        // 🧹 Полная очистка переписки между двумя конкретными людьми
        public async Task<bool> ClearChatHistoryAsync(int senderId, int receiverId)
        {
            string sql = @"DELETE FROM messages 
                   WHERE (sender_id = @SenderId AND receiver_id = @ReceiverId)
                      OR (sender_id = @ReceiverId AND receiver_id = @SenderId);";

            // 1. Очищаем локально
            int rowsAffected = await _context.SqliteConnection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });

            // 2. Очищаем на сервере
            if (_context.IsPostgresAvailable)
            {
                try
                {
                    await _context.PostgresConnection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });
                }
                catch (Exception pgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MsgRepo] Ошибка очистки чата в Postgres: {pgEx.Message}");
                }
            }

            return rowsAffected > 0;
        }
    }
}
