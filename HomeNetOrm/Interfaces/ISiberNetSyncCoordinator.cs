using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetOrm.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using HomeNetCore.Models;

   /// <summary>
        /// Контракт управления фоновой и реалтайм синхронизацией между SQLite и PostgreSQL.
        /// </summary>
        public interface ISiberNetSyncCoordinator
        {
            /// <summary>
            /// Запуск первоначального стягивания (Pull) данных из Postgres в SQLite при старте приложения.
            /// </summary>
            Task StartAsync();

            // =================================================================
            // 👤 РЕКОРДЫ МУТАЦИИ ПОЛЬЗОВАТЕЛЕЙ (Users Sync Signals)
            // =================================================================
            public record UserInserted(UserEntity User);
            public record UserUpdated(UserEntity User);
            public record UserDeleted(Guid UserId);

            // =================================================================
            // 💬 РЕКОРДЫ МУТАЦИИ СООБЩЕНИЙ (Messages Sync Signals)
            // =================================================================
            public record MessageInserted(MessageEntity Message);
            public record MessageUpdated(MessageEntity Message);
            public record MessageDeleted(Guid MessageId);
            public record ChatCleared(Guid SenderId, Guid ReceiverId);
        }
    

}
