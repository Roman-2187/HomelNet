using Dapper;
using HomeNetCore.Exeptions;
using HomeNetCore.Interfaces;
using HomeNetCore.Interfaces.Events; // 🔌 Подключили шину ядра
using HomeNetCore.Models;
using HomeNetOrm.DBProviders.Interfaces;
using HomeNetOrm.Interfaces;
using System.Data.Common;

namespace HomeNetOrm.Repositories
{
    /// <summary>
    /// Автономный репозиторий пользователей SiberNet.
    /// Работает с чистым подключением, генератором и шлёт рекорды синхронизации в шину.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly DbConnection _connection;
        private readonly IUserSqlGenerator _sqlGenerator;
        private readonly IEventBus _eventBus; // Локальная ссылка на автобус

        public UserRepository(DbConnection connection, IUserSqlGenerator sqlGenerator, IEventBus eventBus)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
            }
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsSynced = 0;

            string sql = _sqlGenerator.GenerateInsert();

            try
            {
                await _connection.ExecuteAsync(sql, user);

                // 🔥 ВЫСТРЕЛ В АВТОБУС: Координатор, пушнь нового юзера в Postgres!
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
                    // 🔥 Сигналим об успешном удалении пользователя по Guid
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
                var users = (await _connection.QueryAsync<UserEntity>(sql)).ToList();
                return users;
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
            user.IsSynced = 0; // Сбрасываем флаг

            string sql = _sqlGenerator.GenerateUpdate();

            try
            {
                await _connection.ExecuteAsync(sql, user);

                // 🔥 Сигналим об изменении профиля юзера
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
    }
}
