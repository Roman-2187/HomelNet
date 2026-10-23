using Dapper;
using HomeNetCore.Exeptions;
using HomeNetCore.Extensions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Diagnostics; // Твой логгер
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.Sinc;
using HomeNetCore.Models;
using HomeNetCore.Models.Entity;
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
        private readonly ILogger _logger; // Внедряем поле логгера напрямую

        ILogger ISyncSupport<UserEntity>.Logger => _logger;

        // Явная реализация интерфейса поддержки синхронизации для экстеншенов
        DbConnection ISyncSupport<UserEntity>.Connection => _connection;
        ISqlGenerator<UserEntity> ISyncSupport<UserEntity>.SqlGenerator => _sqlGenerator;

        // 🔥 ЕДИНЫЙ УНИВЕРСАЛЬНЫЙ КОНСТРУКТОР ДЛЯ КЛИЕНТА И СЕРВЕРА С ЛОГГЕРОМ
        public UserRepository(DbConnection connection, IUserSqlGenerator sqlGenerator, ILogger logger, IEventBus? eventBus = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Логгер обязателен
            _eventBus = eventBus ?? new NullEventProxy();
        }

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();

            // 🛡️ РУБЕЖ ОБОРОНЫ: Сначала проверяем существование в СУБД по ID и Email
            var existingUser = await GetByIdAsync(user.Id);
            if (existingUser == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                existingUser = await GetByEmailAsync(user.Email);
            }

            // Если пользователь уже физически существует в СУБД
            if (existingUser != null)
            {
                // Пишем в лог как есть, логгер сам всё раскидает по статусам на турникет 🦾
                _logger.LogWarning($"[UserRepository] Вставка отменена! Пользователь '{user.FirstName}' ({user.Email ?? "Без почты"}) уже существует в БД. (ID: {user.Id})");

                // Публикуем событие дубликата, чтобы серверный хандлер отправил повторную галочку на клиент
                _eventBus.Publish(this, new ISiberNetSyncCoordinator.UserInserted(existingUser));

                return existingUser;
            }

            // ШАГ 2: Если пользователя реально нет в базе — выполняем честный, физический INSERT
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            string sql = _sqlGenerator.GenerateInsert();

            try
            {
                await _connection.ExecuteAsync(sql, user);

                // Честный лог успеха только после записи на диск
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
