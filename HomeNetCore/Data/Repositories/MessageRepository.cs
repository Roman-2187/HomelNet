using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;

namespace HomeNetCore.Data.Repositories
{
    // Подключаем контракт IMessageRepository! 🔌
    public class MessageRepository : IMessageRepository
    {
        private readonly DbConnection _connection;
        private readonly ISqlGenerator<MessageEntity> _sqlGen;

        public MessageRepository(DbConnection connection, ISqlGenerator<MessageEntity> sqlGen)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGen = sqlGen ?? throw new ArgumentNullException(nameof(sqlGen));
        }

        // 💬 Сохранить СМС в базу на автопилоте дженерика!
        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            string sql = _sqlGen.GenerateInsert();
            int rowsAffected = await _connection.ExecuteAsync(sql, message);
            return rowsAffected > 0;
        }

        // 🔍 Выгрузить историю переписки конкретной пары родственников (Вася + Иван)
        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            string sql = @"SELECT * FROM messages 
                           WHERE (sender_id = @SenderId AND receiver_id = @ReceiverId)
                              OR (sender_id = @ReceiverId AND receiver_id = @SenderId)
                           ORDER BY created_at ASC;";

            return await _connection.QueryAsync<MessageEntity>(sql, new { SenderId = senderId, ReceiverId = receiverId });
        }

        // 🧼 Отметить сообщения диалога как прочитанные
        public async Task<bool> MarkAsReadAsync(int senderId, int receiverId)
        {
            string sql = @"UPDATE messages SET is_read = 1 
                           WHERE sender_id = @SenderId AND receiver_id = @ReceiverId AND is_read = 0;";

            int rowsAffected = await _connection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });
            return rowsAffected > 0;
        }

        // 🔢 Считаем, сколько весточек прислал конкретный отправитель текущему вошедшему юзеру
        public async Task<int> GetUnreadCountAsync(int currentUserId, int senderId)
        {
            // Считаем строки, где получатель — Я, отправитель — ОН, а флаг прочтения равен 0 (false)
            string sql = @"SELECT COUNT(*) FROM messages 
                           WHERE receiver_id = @CurrentUserId 
                             AND sender_id = @SenderId 
                             AND is_read = 0;";

            return await _connection.ExecuteScalarAsync<int>(sql, new { CurrentUserId = currentUserId, SenderId = senderId });
        }

        // ❌ Полное физическое удаление конкретного сообщения по его ID (Удалить у всех)
        public async Task<bool> RemoveMessageByIdAsync(int id)
        {
            // Дженерик сам соберёт DELETE FROM messages WHERE id = @id! 🚀
            string sql = _sqlGen.GenerateDelete();
            int rowsAffected = await _connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        // 🧹 Полная очистка переписки между двумя конкретными людьми
        public async Task<bool> ClearChatHistoryAsync(int senderId, int receiverId)
        {
            string sql = @"DELETE FROM messages 
                   WHERE (sender_id = @SenderId AND receiver_id = @ReceiverId)
                      OR (sender_id = @ReceiverId AND receiver_id = @SenderId);";

            int rowsAffected = await _connection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });
            return rowsAffected > 0;
        }

    }
}
