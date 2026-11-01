using Dapper;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Exeptions;
using HomeNetCore.Interfaces;
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
                _eventBus.Publish(this, new ISyncCoordinator.UserInserted(existingUser));
                return existingUser;
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            var cmd = _userSqlGenerator.GenerateInsert();

            try
            {
                Task DbAction(DbConnection db)
                {
                    return db.ExecuteAsync(cmd.Sql, user);
                }

                await ExecuteAsync(DbAction);
                Logger.LogInfo($"[UserRepository] Пользователь '{user.FirstName}' ({user.Email}) успешно записан в БД.");
                _eventBus.Publish(this, new ISyncCoordinator.UserInserted(user));
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
            int affectedRows = 0;

            try
            {
                Task<int> DbAction(DbConnection db)
                {
                    return db.ExecuteAsync(cmd.Sql, cmd.Params);
                }

                affectedRows = await ExecuteAsync(DbAction);
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                throw new InvalidOperationException($"Ошибка при удалении пользователя через ОРМ: {ex.Message}");
            }

            // Публикация вынесена из лямбды наружу
            if (affectedRows > 0)
            {
                _eventBus.Publish(this, new ISyncCoordinator.UserDeleted(id));
            }
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
            var cmd = _userSqlGenerator.GenerateSelectAll();
            try
            {
                async Task<List<UserEntity>> DbAction(DbConnection db)
                {
                    var result = await db.QueryAsync<UserEntity>(cmd.Sql);
                    return result.ToList();
                }

                return await ExecuteAsync(DbAction);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось выполнить запрос получения пользователей через ОРМ: {ex.Message}");
            }
        }

        public async Task<UserEntity?> GetByIdAsync(Guid id)
        {
            var cmd = _userSqlGenerator.GenerateSelectById(id);

            Task<UserEntity?> DbAction(DbConnection db)
            {
                return db.QueryFirstOrDefaultAsync<UserEntity>(cmd.Sql, cmd.Params);
            }

            return await ExecuteAsync(DbAction);
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            var cmd = _userSqlGenerator.GenerateSelectByEmail(email);

            Task<UserEntity?> DbAction(DbConnection db)
            {
                return db.QueryFirstOrDefaultAsync<UserEntity>(cmd.Sql, cmd.Params);
            }

            return await ExecuteAsync(DbAction);
        }

        public async Task UpdateAsync(UserEntity user)
        {
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            var cmd = _userSqlGenerator.GenerateUpdate();
            try
            {
                Task DbAction(DbConnection db)
                {
                    return db.ExecuteAsync(cmd.Sql, user);
                }

                await ExecuteAsync(DbAction);
                _eventBus.Publish(this, new ISyncCoordinator.UserUpdated(user));
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

            async Task<bool> DbAction(DbConnection db)
            {
                var count = await db.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params);
                return count > 0;
            }

            return await ExecuteAsync(DbAction);
        }

        private class NullEventProxy : IEventBus { }
    }
}
