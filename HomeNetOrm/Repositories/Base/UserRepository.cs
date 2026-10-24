using Dapper;
using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    /// <summary>
    /// Автономный репозиторий пользователей SiberNet.
    /// Полностью на автопилоте объектов команд без единой строки хардкода параметров! 🦾
    /// </summary>
    public class UserRepository : IUserRepository, ISyncSupport<UserEntity>
    {
        private readonly DbConnection _connection;
        private readonly IUserSqlGenerator _sqlGenerator;
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;

        ILogger ISyncSupport<UserEntity>.Logger => _logger;
        DbConnection ISyncSupport<UserEntity>.Connection => _connection;
        ISqlGenerator<UserEntity> ISyncSupport<UserEntity>.SqlGenerator => _sqlGenerator;

        internal UserRepository(DbConnection connection, IUserSqlGenerator sqlGenerator, ILogger logger, IEventBus? eventBus = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.LogWarning($"[UserRepository] Вставка отменена! Пользователь '{user.FirstName}' ({user.Email ?? "Без почты"}) уже существует в БД. (ID: {user.Id})");
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.UserInserted(existingUser));
                return existingUser;
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            // 🎯 Для Insert/Update по-прежнему отдаем команду, но Dapper-у скармливаем сам объект user,
            // так как SqlQueryBuilder привязал параметры INSERT к свойствам C#-класса!
            var cmd = _sqlGenerator.GenerateInsert();

            try
            {
                await _connection.ExecuteAsync(cmd.Sql, user);
                _logger.LogInfo($"[UserRepository] Пользователь '{user.FirstName}' ({user.Email}) успешно записан в БД.");
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
            // 🎯 АВТОПИЛОТ: Передаем id в генератор, забираем готовый объект команды
            var cmd = _sqlGenerator.GenerateDelete(id);
            try
            {
                var affectedRows = await _connection.ExecuteAsync(cmd.Sql, cmd.Params);
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
            var cmd = _sqlGenerator.GenerateSelectAll();
            try
            {
                return (await _connection.QueryAsync<UserEntity>(cmd.Sql)).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось выполнить запрос получения пользователей через ОРМ: {ex.Message}");
            }
        }

        public async Task<UserEntity?> GetByIdAsync(Guid id)
        {
            // 🎯 ЧИСТЫЙ ВЫЗОВ ЧЕРЕЗ ТОЧКУ: Никакого ручного создания DynamicParameters или анонимных типов!
            var cmd = _sqlGenerator.GenerateSelectById(id);
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(cmd.Sql, cmd.Params);
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            // 🎯 Финал регистровой паранойи: Забрали команду, кинули в Dapper через точку
            var cmd = _sqlGenerator.GenerateSelectByEmail(email);
            return await _connection.QueryFirstOrDefaultAsync<UserEntity>(cmd.Sql, cmd.Params);
        }

        public async Task UpdateAsync(UserEntity user)
        {
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            var cmd = _sqlGenerator.GenerateUpdate();
            try
            {
                await _connection.ExecuteAsync(cmd.Sql, user);
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

            var cmd = _sqlGenerator.GenerateEmailExists(email);
            var count = await _connection.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params);
            return count > 0;
        }

        private class NullEventProxy : IEventBus { }
    }
}
