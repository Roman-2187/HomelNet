using System.Data.Common;
using Dapper;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using HomeNetOrm.Data.Builders;
using HomeNetOrm.Enums;

namespace HomeNetOrm.Data.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        // 🔥 Вместо жестких ссылок берем динамический контейнер контекста СУБД!
        private readonly DbContextContainer _context;

        public MessageRepository(DbContextContainer context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // 💬 Сохранить СМС в базу на автопилоте дженерика!
        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            try
            {
                DbConnection connection = _context.Connection;
                string sql = _context.MessageSqlGen.GenerateInsert(); // Берем insert из активной СУБД
                int rowsAffected = await connection.ExecuteAsync(sql, message);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка сохранения сообщения: {ex.Message}");
            }
        }

        // 🔍 Выгрузить историю переписки конкретной пары (Вася + Иван) с защитой от падения базы
        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            try
            {
                DbConnection connection = _context.Connection;
                string sql = _context.MessageSqlGen.GenerateSelectChatHistory();

                return await connection.QueryAsync<MessageEntity>(sql, new { userId = senderId, friendId = receiverId });
            }
            catch (Exception ex) when (ex.Message.Contains("stream") || ex.Message.Contains("connection"))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Сбой БД при чтении чата! Прыгаем на резерв...");

                var fallbackType = _context.CurrentType == DatabaseType.SQLite
                    ? DatabaseType.PostGreSQL
                    : DatabaseType.SQLite;

                await _context.SwitchDatabaseAsync(fallbackType);

                DbConnection connection = _context.Connection;
                string sql = _context.MessageSqlGen.GenerateSelectChatHistory();
                return await connection.QueryAsync<MessageEntity>(sql, new { userId = senderId, friendId = receiverId });
            }
        }

        // 🧼 Отметить сообщения диалога как прочитанные
        public async Task<bool> MarkAsReadAsync(int senderId, int receiverId)
        {
            DbConnection connection = _context.Connection;
            string sql = @"UPDATE messages SET is_read = 1 
                           WHERE sender_id = @SenderId AND receiver_id = @ReceiverId AND is_read = 0;";

            int rowsAffected = await connection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });
            return rowsAffected > 0;
        }

        // 🔢 Считаем, сколько весточек прислал конкретный отправитель текущему вошедшему юзеру
        public async Task<int> GetUnreadCountAsync(int currentUserId, int senderId)
        {
            DbConnection connection = _context.Connection;
            string sql = @"SELECT COUNT(*) FROM messages 
                           WHERE receiver_id = @CurrentUserId 
                             AND sender_id = @SenderId 
                             AND is_read = 0;";

            return await connection.ExecuteScalarAsync<int>(sql, new { CurrentUserId = currentUserId, SenderId = senderId });
        }

        // ❌ Полное физическое удаление конкретного сообщения по его ID (Удалить у всех)
        public async Task<bool> RemoveMessageByIdAsync(int id)
        {
            DbConnection connection = _context.Connection;
            string sql = _context.MessageSqlGen.GenerateDelete();
            int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        // 🧹 Полная очистка переписки между двумя конкретными людьми
        public async Task<bool> ClearChatHistoryAsync(int senderId, int receiverId)
        {
            DbConnection connection = _context.Connection;
            string sql = @"DELETE FROM messages 
                   WHERE (sender_id = @SenderId AND receiver_id = @ReceiverId)
                      OR (sender_id = @ReceiverId AND receiver_id = @SenderId);";

            int rowsAffected = await connection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });
            return rowsAffected > 0;
        }
    }
}
