using Dapper;
using HomeNetCore.Exeptions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Автономный репозиторий пользователей SiberNet.
    /// Полностью очищен от привязки к IDbEngine и использует дефолтную реализацию шины! 🦾
    /// </summary>
    public class UserRepository : IUserRepository, ISyncSupport<UserEntity>
    {
        private readonly DbConnection _connection;
        private readonly IUserSqlGenerator _sqlGenerator;
        private readonly IEventBus _eventBus;

        // Явная реализация интерфейса поддержки синхронизации для экстеншенов
        DbConnection ISyncSupport<UserEntity>.Connection => _connection;
        ISqlGenerator<UserEntity> ISyncSupport<UserEntity>.SqlGenerator => _sqlGenerator;

        // 🔥 ЕДИНЫЙ УНИВЕРСАЛЬНЫЙ КОНСТРУКТОР ДЛЯ КЛИЕНТА И СЕРВЕРА
        public UserRepository(DbConnection connection, IUserSqlGenerator sqlGenerator, IEventBus? eventBus = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? new NullEventProxy();
        }

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            string sql = _sqlGenerator.GenerateInsert();

            try
            {
                await _connection.ExecuteAsync(sql, user);
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.UserInserted(user));
                return user;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при вставке пользователя через ОРМ: {ex.Message}");
            }
        }

       

        public async Task DeleteByIdAsync(Guid id)
        {
            string sql = _sqlGenerator.GenerateDelete();
            try
            {
                var affectedRows = await _connection.ExecuteAsync(sql, new { id = id });
                if (affectedRows > 0)
                {
                    _eventBus.Publish(this, new ISiberNetSyncCoordinator.UserDeleted(id));
                }
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                throw new InvalidOperationException($"Ошибка при удалении пользователя через ОРМ: {ex.Message}");
            }
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
            string sql = _sqlGenerator.GenerateSelectAll();
            try
            {
                return (await _connection.QueryAsync<UserEntity>(sql)).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось выполнить запрос получения пользователей через ОРМ: {ex.Message}");
            }
        }

        public async Task<UserEntity?> GetByIdAsync(Guid id)
        {
            string sql = _sqlGenerator.GenerateSelectById();
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { id = id });
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            string sql = _sqlGenerator.GenerateSelectByEmail();
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { email = email });
        }

        public async Task UpdateAsync(UserEntity user)
        {
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            string sql = _sqlGenerator.GenerateUpdate();
            try
            {
                await _connection.ExecuteAsync(sql, user);
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.UserUpdated(user));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка обновления пользователя через ОРМ: {ex.Message}");
            }
        }

        public async Task<bool> EmailExistsAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            string sql = _sqlGenerator.GenerateEmailExists();
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { email = email });
            return count > 0;
        }

        // Минимальный скрытый приватный прокси-класс
        private class NullEventProxy : IEventBus { }
    }
}
