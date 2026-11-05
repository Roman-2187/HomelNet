using Dapper;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Extensions;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    // 🔥 Добавляем IDisposable, чтобы можно было освобождать ресурсы
    public class UserRepository : BaseRepository<UserEntity>, IUserRepo, IDisposable
    {
        private readonly IUserSqlGenerator _userSqlGenerator;
        private readonly IInternalEventBus? _eventBus;

        // 🖥️ СЕРВЕРНЫЙ КОНСТРУКТОР (без шины)
        internal UserRepository(Func<DbConnection> connectionFactory, IUserSqlGenerator sqlGenerator, ILogger logger)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _userSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
           
        }

        // 📱 КЛИЕНТСКИЙ КОНСТРУКТОР (с шиной)
        internal UserRepository(Func<DbConnection> connectionFactory, IUserSqlGenerator sqlGenerator, ILogger logger, IInternalEventBus eventBus)
            : this(connectionFactory, sqlGenerator, logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            SubscribeToEvents(); // 🔥 Вызываем метод подписки
        }

        // =========================================================================
        // 📥 ПОДПИСКА И ОТПИСКА
        // =========================================================================

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Subscribe<IUserRepo.InsertRequested>(OnInsertRequested);
            _eventBus.Subscribe<IUserRepo.UpdateRequested>(OnUpdateRequested);
            _eventBus.Subscribe<IUserRepo.DeleteRequested>(OnDeleteRequested);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<IUserRepo.InsertRequested>(OnInsertRequested);
            _eventBus.Unsubscribe<IUserRepo.UpdateRequested>(OnUpdateRequested);
            _eventBus.Unsubscribe<IUserRepo.DeleteRequested>(OnDeleteRequested);
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
        }

        // =========================================================================
        // 📥 ОБРАБОТЧИКИ КОМАНД
        // =========================================================================

        private async void OnInsertRequested(IUserRepo.InsertRequested cmd)
        {
            if (cmd?.User == null) return;
            try { await InsertAsync(cmd.User); }
            catch (Exception ex) { Logger.LogError($"[UserRepository] Ошибка InsertRequested: {ex.Message}"); }
        }

        private async void OnUpdateRequested(IUserRepo.UpdateRequested cmd)
        {
            if (cmd?.User == null) return;
            try { await UpdateAsync(cmd.User); }
            catch (Exception ex) { Logger.LogError($"[UserRepository] Ошибка UpdateRequested: {ex.Message}"); }
        }

        private async void OnDeleteRequested(IUserRepo.DeleteRequested cmd)
        {
            if (cmd == null || cmd.UserId == Guid.Empty) return;
            try { await DeleteByIdAsync(cmd.UserId); }
            catch (Exception ex) { Logger.LogError($"[UserRepository] Ошибка DeleteRequested: {ex.Message}"); }
        }

        // =========================================================================
        // 🔥 РЕАЛИЗАЦИЯ КОНТРАКТА ICrudRepository (с фактами)
        // =========================================================================

        public async Task<UserEntity> InsertAsync(UserEntity user)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();

            var existingUser = await GetByIdAsync(user.Id);
            if (existingUser == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                existingUser = await GetByEmailAsync(user.Email);
            }

            if (existingUser != null)
            {
                Logger.LogWarning($"[UserRepository] Пользователь уже существует. (ID: {user.Id}). Публикация Inserted пропущена.");
                return existingUser;
            }

            var cmd = _userSqlGenerator.GenerateInsert();
            try
            {
                await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, user));
                Logger.LogInfo($"[UserRepository] Пользователь '{user.FirstName}' успешно записан.");

                _eventBus?.PublishFact(this, new IUserRepo.Inserted(user, "Успешно записан"));
                return user;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка ОРМ вставки: {ex.Message}");
            }
        }

        public async Task<UserEntity?> GetByIdAsync(Guid id)
        {
            var cmd = _userSqlGenerator.GenerateSelectById(id);
            return await ExecuteAsync(db => db.QueryFirstOrDefaultAsync<UserEntity>(cmd.Sql, cmd.Params));
        }

        public async Task<IEnumerable<UserEntity>> GetAllAsync()
        {
            var cmd = _userSqlGenerator.GenerateSelectAll();
            return await ExecuteAsync(db => db.QueryAsync<UserEntity>(cmd.Sql, cmd.Params));
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            var cmd = _userSqlGenerator.GenerateDelete(id);
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));

            if (rowsAffected > 0)
            {
                _eventBus?.PublishFact(this, new IUserRepo.Deleted(id));
            }
        }

        public async Task UpdateAsync(UserEntity entity)
        {
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            var cmd = _userSqlGenerator.GenerateUpdate();
            int rowsAffected = await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, entity));

            if (rowsAffected > 0)
            {
                _eventBus?.PublishFact(this, new IUserRepo.Updated(entity, "Обновлен"));
            }
        }

        // =========================================================================
        // 🔍 СПЕЦИФИЧНЫЕ МЕТОДЫ
        // =========================================================================

        public async Task<UserEntity?> GetByEmailAsync(string email) =>
            await ExecuteAsync(db => 
            db.QueryFirstOrDefaultAsync<UserEntity>(_userSqlGenerator.GenerateSelectByEmail(email).Sql, new { Email = email }));

        public async Task<bool> EmailExistsAsync(string? email) => !string.IsNullOrWhiteSpace(email) && await ExecuteAsync(async db =>
            await db.ExecuteScalarAsync<int>(_userSqlGenerator.GenerateEmailExists(email).Sql, new { Email = email }) > 0);
    }
}