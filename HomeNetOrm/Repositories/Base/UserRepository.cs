using Dapper;
using HomeNet.Infrastructure.Shared;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Exeptions;
using HomeNetOrm.Generators.Extensions;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    public class UserRepository : BaseRepository<UserEntity>, IUserRepo
    {
        private readonly IUserSqlGenerator _userSqlGenerator;
        private readonly IEventBus? _eventBus;
        private readonly IDbExceptionHandler _errorHandler;

        // 🖥️ СЕРВЕРНЫЙ КОНСТРУКТОР (без шины)
        internal UserRepository(Func<DbConnection> connectionFactory, IUserSqlGenerator sqlGenerator, ICastomLogger logger, IDbExceptionHandler errorHandler)
            : base(connectionFactory, sqlGenerator, logger)
        {
            _userSqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        // 📱 КЛИЕНТСКИЙ КОНСТРУКТОР (с шиной)
        internal UserRepository(Func<DbConnection> connectionFactory, IUserSqlGenerator sqlGenerator, ICastomLogger logger, IEventBus eventBus, IDbExceptionHandler errorHandler)
            : this(connectionFactory, sqlGenerator, logger, errorHandler)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        // 🔥 Вставка осталась с ручной обработкой (т.к. кидаем DuplicateEmailException)
        public async Task<UserEntity> InsertAsync(UserEntity user)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();

            var cmd = _userSqlGenerator.GenerateInsert();

            try
            {
                await ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, user));
                Logger.LogInfo($"[UserRepository] Пользователь '{user.FirstName}' успешно записан.");

                _eventBus?.Publish(this, new IUserRepo.Inserted(user, "Успешно записан"));
                return user;
            }
            catch (Exception ex) when (_errorHandler.IsDuplicateKeyException(ex))
            {
                throw new DuplicateEmailException(user.Email!);
            }
            catch (Exception ex)
            {
                throw _errorHandler.Translate(ex);
            }
        }

        // 🔥 ОСТАЛЬНЫЕ МЕТОДЫ ОБЕРНУТЫ В ПЕРЕХВАТЧИК

        public async Task UpdateAsync(UserEntity entity)
        {
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            var cmd = _userSqlGenerator.GenerateUpdate();

            int rowsAffected = await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, entity)));

            if (rowsAffected > 0)
            {
                _eventBus?.Publish(this, new IUserRepo.Updated(entity, "Обновлен"));
            }
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            var cmd = _userSqlGenerator.GenerateDelete(id);

            int rowsAffected = await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params)));

            if (rowsAffected > 0)
            {
                _eventBus?.Publish(this, new IUserRepo.Deleted(id));
            }
        }

        public async Task<UserEntity?> GetByIdAsync(Guid id)
        {
            var cmd = _userSqlGenerator.GenerateSelectById(id);

            return await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.QueryFirstOrDefaultAsync<UserEntity>(cmd.Sql, cmd.Params)));
        }

        public async Task<IEnumerable<UserEntity>> GetAllAsync()
        {
            var cmd = _userSqlGenerator.GenerateSelectAll();

            return await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.QueryAsync<UserEntity>(cmd.Sql, cmd.Params)));
        }

        public async Task<UserEntity?> GetByEmailAsync(string email) =>
            await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(db => db.QueryFirstOrDefaultAsync<UserEntity>(
                    _userSqlGenerator.GenerateSelectByEmail(email).Sql, new { Email = email })));

        public async Task<bool> EmailExistsAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await AutoExceptionHandler.ExecuteThrowAsync(Logger, () =>
                ExecuteAsync(async db => await db.ExecuteScalarAsync<int>(
                    _userSqlGenerator.GenerateEmailExists(email).Sql, new { Email = email }) > 0));
        }
    }
}