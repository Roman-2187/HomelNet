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
        }

        private record UserInsertedFact(UserEntity User, string? Message) : IUserRepo.IInserted;

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();

            // Пользуемся базовым CRUD-экстеншеном GetByIdAsync внутри самого репо!
            var existingUser = await this.GetByIdAsync<UserEntity>(user.Id);
            if (existingUser == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                existingUser = await GetByEmailAsync(user.Email);
            }

            if (existingUser != null)
            {
                Logger.LogWarning($"[UserRepository] Пользователь уже существует. (ID: {user.Id})");
                _eventBus.Publish(this, new UserInsertedFact(existingUser, "Существует"));
                return existingUser;
            }

            user.LocalCreatedAt = DateTimeOffset.UtcNow;
            user.ServerCreatedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            var cmd = _userSqlGenerator.GenerateInsert();
            try
            {
                await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, user));
                Logger.LogInfo($"[UserRepository] Пользователь '{user.FirstName}' успешно записан.");
                _eventBus.Publish(this, new UserInsertedFact(user, "Успешно записан"));
                return user;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка ОРМ вставки: {ex.Message}");
            }
        }

        // Чисто уникальные методы выборки юзеров по бизнес-полям (Email)
        public async Task<UserEntity?> GetByEmailAsync(string email) =>
            await ExecuteAsync(db => db.QueryFirstOrDefaultAsync<UserEntity>(_userSqlGenerator.GenerateSelectByEmail(email).Sql, new { Email = email }));

        public async Task<bool> EmailExistsAsync(string? email) =>
            string.IsNullOrWhiteSpace(email) ? false : await ExecuteAsync(async db => await db.ExecuteScalarAsync<int>(_userSqlGenerator.GenerateEmailExists(email).Sql, new { Email = email }) > 0);

        private class NullEventProxy : IEventBus { }
    }
}
