using Dapper;
using HomeNetCore.Interfaces.Events; // 🔌 Подключили шину ядра
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces.HomeNetOrm.Interfaces;
using System.Data.Common;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Автономный репозиторий сообщений SiberNet.
    /// Работает на чистом подключении и специализированном генераторе сообщений. Пушит сигналы репликации в шину.
    /// </summary>
    public class MessageRepository : IMessageRepository
    {
        private readonly DbConnection _connection;
        private readonly IMessageSqlGenerator _sqlGenerator;
        private readonly IEventBus _eventBus; // Ссылка на глобальный автобус

        public MessageRepository(DbConnection connection, IMessageSqlGenerator sqlGenerator, IEventBus eventBus)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        // 💬 Сохранить сообщение через ОРМ
        public async Task<bool> SaveMessageAsync(MessageEntity message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.IsSynced = 0; // По умолчанию запись локальная и "грязная"

            string sql = _sqlGenerator.GenerateInsert();

            try
            {
                var newId = await _connection.ExecuteScalarAsync<Guid>(sql, message);
                message.Id = newId;

                // 🔥 ВЫСТРЕЛ В АВТОБУС: Координатор синхронизации, забирай мессагу в Postgres!
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.MessageInserted(message));

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка сохранения сообщения через ОРМ: {ex.Message}");
            }
        }

        // 🔍 Выгрузить историю переписки конкретной пары по схемам ОРМ
        public async Task<IEnumerable<MessageEntity>> GetChatHistoryAsync(int senderId, int receiverId)
        {
            string sql = _sqlGenerator.GenerateSelectChatHistory();
            try
            {
                return await _connection.QueryAsync<MessageEntity>(sql, new { userId = senderId, friendId = receiverId });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось получить историю чата через ОРМ: {ex.Message}");
            }
        }

        // 🧼 Отметить сообщения диалога как прочитанные через ОРМ
        public async Task<bool> MarkAsReadAsync(int senderId, int receiverId)
        {
            string sql = _sqlGenerator.GenerateMarkAsRead();

            try
            {
                int rowsAffected = await _connection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });

                if (rowsAffected > 0)
                {
                    // Вытаскиваем измененные строки или генерируем событие апдейта диалога
                    // Для примера шлем пустой апдейт или конкретную сущность
                    // _eventBus.Publish(this, new ISiberNetSyncCoordinator.MessageUpdated(...));
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка обновления статуса прочтения сообщений через ОРМ: {ex.Message}");
            }
        }

        // 🔢 Считаем количество непрочитанных через ОРМ
        public async Task<int> GetUnreadCountAsync(int currentUserId, int senderId)
        {
            string sql = _sqlGenerator.GenerateUnreadCount();

            try
            {
                return await _connection.ExecuteScalarAsync<int>(sql, new { CurrentUserId = currentUserId, SenderId = senderId });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка подсчета непрочитанных через ОРМ: {ex.Message}");
            }
        }

        // ❌ Удаление конкретного сообщения по ID через ОРМ
        public async Task<bool> RemoveMessageByIdAsync(int id)
        {
            // Сначала вытаскиваем Guid сообщения, чтобы репликатор на сервере знал, что стирать
            var message = await _connection.QueryFirstOrDefaultAsync<MessageEntity>(
                _sqlGenerator.GenerateSelectById(), new { id = id });

            string sql = _sqlGenerator.GenerateDelete();
            try
            {
                int rowsAffected = await _connection.ExecuteAsync(sql, new { id = id });

                if (rowsAffected > 0 && message != null)
                {
                    // 🔥 Сигналим об успешном удалении по Guid
                    _eventBus.Publish(this, new ISiberNetSyncCoordinator.MessageDeleted(message.Id));
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка удаления сообщения по ID через ОРМ: {ex.Message}");
            }
        }

        // 🧹 Полная очистка переписки между двумя людьми через ОРМ
        public async Task<bool> ClearChatHistoryAsync(int senderId, int receiverId)
        {
            string sql = _sqlGenerator.GenerateClearChatHistory();

            try
            {
                // Вытаскиваем Guid участников для передачи в рекорд
                var sender = await _connection.QueryFirstOrDefaultAsync<UserEntity>("SELECT id FROM users WHERE id = @id", new { id = senderId });
                var receiver = await _connection.QueryFirstOrDefaultAsync<UserEntity>("SELECT id FROM users WHERE id = @id", new { id = receiverId });

                int rowsAffected = await _connection.ExecuteAsync(sql, new { SenderId = senderId, ReceiverId = receiverId });

                if (rowsAffected > 0 && sender != null && receiver != null)
                {
                    // 🔥 Сигналим о полной очистке ветки чата
                    _eventBus.Publish(this, new ISiberNetSyncCoordinator.ChatCleared(sender.Id, receiver.Id));
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при полной очистке чата через ОРМ: {ex.Message}");
            }
        }
    }
}
