using Dapper;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Repositories;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Builders;
using System.Data.Common;
using HomeNetOrm.Interfaces.Sinc;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Автономный репозиторий списка друзей/контактов SiberNet.
    /// Выполняет CRUD-операции через инжектируемый движок СУБД и шлет события в EventBus.
    /// </summary>
    public class FriendRepository : IFriendRepository
    {
        private readonly DbConnection _connection;
        private readonly ISqlGenerator<FriendEntity> _sqlGenerator;
        private readonly IEventBus _eventBus; // Добавили шину событий

        public FriendRepository(IDbEngine dbEngine, IEventBus eventBus)
        {
            if (dbEngine == null) throw new ArgumentNullException(nameof(dbEngine));

            _connection = dbEngine.Connection;
            _sqlGenerator = dbEngine.GetGenerator<FriendEntity>();
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<bool> AddFriendAsync(FriendEntity friend)
        {
            if (friend.Id == Guid.Empty) friend.Id = Guid.NewGuid();
            friend.CreatedAt = DateTime.UtcNow;
            friend.UpdatedAt = DateTimeOffset.UtcNow;
            friend.IsSynced = 0;

            string sql = _sqlGenerator.GenerateInsert();

            try
            {
                var newId = await _connection.ExecuteScalarAsync<Guid>(sql, friend);
                if (newId != Guid.Empty) friend.Id = newId;

                // 🔥 ВЫСТРЕЛ В АВТОБУС: Синхронизатор, тащи новый контакт на Postgres сервер!
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.FriendInserted(friend));

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при добавлении в друзья через ОРМ: {ex.Message}");
            }
        }

        public async Task<bool> RemoveFriendByIdAsync(Guid id)
        {
            string sql = _sqlGenerator.GenerateDelete();

            try
            {
                int rowsAffected = await _connection.ExecuteAsync(sql, new { id = id });

                if (rowsAffected > 0)
                {
                    // 🔥 Сигналим об удалении контакта по Guid для репликатора сервера
                    _eventBus.Publish(this, new ISiberNetSyncCoordinator.FriendDeleted(id));
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка удаления связи контактов через ОРМ: {ex.Message}");
            }
        }

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
