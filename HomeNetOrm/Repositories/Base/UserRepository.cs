using Dapper;
using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    public class UserRepository : BaseRepository<UserEntity>, IUserRepository
    {
        private readonly IUserSqlGenerator _userSqlGenerator;
        private readonly IEventBus _eventBus;

        internal UserRepository(Func<DbConnection> connectionFactory, IUserSqlGenerator sqlGenerator, ILogger logger, IEventBus? eventBus = null)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _userSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? new NullEventProxy();
        }

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();

            var existingUser = await GetByIdAsync(user.Id);
            if (existingUser == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                existingUser = await GetByEmailAsync(user.Email);
            }

            if (existingUser != null)
            {
                Logger.LogWarning($"[UserRepository] Вставка отменена! Пользователь '{user.FirstName}' ({user.Email ?? "Без почты"}) уже существует в БД. (ID: {user.Id})");
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.UserInserted(existingUser));
                return existingUser;
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            var cmd = _userSqlGenerator.GenerateInsert();

            try
            {
                await ExecuteAsync(async db => await db.ExecuteAsync(cmd.Sql, user));
                Logger.LogInfo($"[UserRepository] Пользователь '{user.FirstName}' ({user.Email}) успешно записан в БД.");
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
            var cmd = _userSqlGenerator.GenerateDelete(id);
            try
            {
                await ExecuteAsync(async db =>
                {
                    var affectedRows = await db.ExecuteAsync(cmd.Sql, cmd.Params);
                    if (affectedRows > 0)
                    {
                        _eventBus.Publish(this, new ISiberNetSyncCoordinator.UserDeleted(id));
                    }
                });
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                throw new InvalidOperationException($"Ошибка при удалении пользователя через ОРМ: {ex.Message}");
            }
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
            var cmd = _userSqlGenerator.GenerateSelectAll();
            try
            {
                return await ExecuteAsync(async db => (await db.QueryAsync<UserEntity>(cmd.Sql)).ToList());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось выполнить запрос получения пользователей через ОРМ: {ex.Message}");
            }
        }

        public async Task<UserEntity?> GetByIdAsync(Guid id)
        {
            var cmd = _userSqlGenerator.GenerateSelectById(id);
            return await ExecuteAsync(async db => await db.QueryFirstOrDefaultAsync<UserEntity>(cmd.Sql, cmd.Params));
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            var cmd = _userSqlGenerator.GenerateSelectByEmail(email);
            return await ExecuteAsync(async db => await db.QueryFirstOrDefaultAsync<UserEntity>(cmd.Sql, cmd.Params));
        }

        public async Task UpdateAsync(UserEntity user)
        {
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            var cmd = _userSqlGenerator.GenerateUpdate();
            try
            {
                await ExecuteAsync(async db => await db.ExecuteAsync(cmd.Sql, user));
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

            var cmd = _userSqlGenerator.GenerateEmailExists(email);
            return await ExecuteAsync(async db =>
            {
                var count = await db.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params);
                return count > 0;
            });
        }

        private class NullEventProxy : IEventBus { }
    }
}
