using Dapper;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Extensions;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    public class UserRepository : BaseRepository<UserEntity>, IUserRepo
    {
        private readonly IUserSqlGenerator _userSqlGenerator;
        private readonly IEventBus _eventBus;

        internal UserRepository(Func<DbConnection> connectionFactory, IUserSqlGenerator sqlGenerator, ILogger logger, IEventBus? eventBus = null)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _userSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? new NullEventProxy();

            // 🔥 Подписываемся на команду вставки (из сети или других источников)
            if (_eventBus is not NullEventProxy)
            {
                _eventBus.Subscribe<IUserRepo.InsertRequested>(OnInsertRequested);
            }
        }

        private async void OnInsertRequested(IUserRepo.InsertRequested cmd)
        {
            if (cmd?.User == null) return;

            try
            {
                await InsertUserAsync(cmd.User);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserRepository] Ошибка при обработке InsertRequested: {ex.Message}");
            }
        }

        /// <summary>
        /// Публичный метод вставки. Если юзер реально вставлен — публикует Inserted.
        /// Если уже существует — НЕ публикует (чтобы не спамить UI).
        /// </summary>
        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();

            var existingUser = await this.GetByIdAsync<UserEntity>(user.Id);
            if (existingUser == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                existingUser = await GetByEmailAsync(user.Email);
            }

            if (existingUser != null)
            {
                // 🔥 Юзер уже в базе. НИЧЕГО не публикуем — просто возвращаем.
                Logger.LogWarning($"[UserRepository] Пользователь уже существует. (ID: {user.Id}). Публикация Inserted пропущена.");
                return existingUser;
            }

            // 🔥 НЕ трогаем user.IsSynced! Позволяем вызывающему коду решать:
            // - локальная регистрация → IsSynced = 0 (по умолчанию)
            // - из сети → IsSynced = 1 (ставит UserNetworkHandler)

          

            var cmd = _userSqlGenerator.GenerateInsert();
            try
            {
                await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, user));
                Logger.LogInfo($"[UserRepository] Пользователь '{user.FirstName}' успешно записан.");

                // 🔥 Публикуем факт ВСЕГДА, когда вставка реально произошла.
                // IsSynced внутри юзера уже тот, что нужен (0 или 1).
                _eventBus.Publish(this, new IUserRepo.Inserted(user, "Успешно записан"));
                return user;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка ОРМ вставки: {ex.Message}");
            }
        }

        public async Task<UserEntity?> GetByEmailAsync(string email) =>
            await ExecuteAsync(db => db.QueryFirstOrDefaultAsync<UserEntity>(_userSqlGenerator.GenerateSelectByEmail(email).Sql, new { Email = email }));

        public async Task<bool> EmailExistsAsync(string? email) =>
            string.IsNullOrWhiteSpace(email) ? false : await ExecuteAsync(async db => await db.ExecuteScalarAsync<int>(_userSqlGenerator.GenerateEmailExists(email).Sql, new { Email = email }) > 0);

        private class NullEventProxy : IEventBus { }
    }
}