using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using HomeNetCore.Data.Interfaces;
using HomeNetCore.Models;
using HomeNetCore.Data.DBProviders.Sqlite;
using HomeNetCore.Data.DBProviders.Sqlite.HomeNetCore.Data.DBProviders.Sqlite;

namespace HomeNetCore.Data.Repositories
{
    public class MessageRepository
    {
        private readonly DbConnection _connection;
        private readonly ISqLiteSqlGenerator<MessageEntity> _sqlGen;

        public MessageRepository(DbConnection connection, ISqLiteSqlGenerator<MessageEntity> sqlGen)
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
            // Берем сообщения и от меня к нему, и от него ко мне, сортируя по времени!
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
    }
}

