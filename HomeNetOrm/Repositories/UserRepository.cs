using Dapper;
using HomeNetCore.Interfaces;
using HomeNetCore.Models;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using HomeNetCore.Exeptions;

namespace HomeNetOrm.Repositories
{
    public class UserRepository : IUserRepository
    {
        // 🔥 Храним ссылку на динамический контейнер контекста вместо жёстких ссылок!
        private readonly DbContextContainer _context;

        // В конструкторе принимаем только контекст
        public UserRepository(DbContextContainer context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<UserEntity> InsertUserAsync(UserEntity user)
        {
            try
            {
                DbConnection connection = _context.Connection;
                var sql = _context.UserSqlGen.GenerateInsert();

                // 🎯 ФИКС: Перед вставкой генерируем новый Guid, если он еще не задан
                if (user.Id == Guid.Empty)
                {
                    user.Id = Guid.NewGuid();
                }

                // Штампуем актуальное время перед отправкой в шину/базу
                user.UpdatedAt = DateTimeOffset.UtcNow;

                // Так как ID теперь Guid и создается на клиенте, используем ExecuteAsync вместо ExecuteScalarAsync<int>
                await connection.ExecuteAsync(sql, user);
                return user;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"Ошибка при вставке: {ex.Message}");
            }
        }

        // 🎯 ТЕПЕРЬ ТУТ ЧЕСТНЫЙ Guid вместо int
        public async Task DeleteByIdAsync(Guid id)
        {
            DbConnection connection = _context.Connection;
            var sql = _context.UserSqlGen.GenerateDelete();

            var affectedRows = await connection.ExecuteAsync(sql, new { id = id });

            if (affectedRows == 0)
            {
                throw new NotFoundException($"Пользователь с ID {id} не найден.");
            }
        }

        public async Task<List<UserEntity>> GetAllAsync()
        {
            DbConnection connection = _context.Connection;
            string sql = _context.UserSqlGen.GenerateSelectAll();

            try
            {
                var users = (await connection.QueryAsync<UserEntity>(sql)).ToList();
                return users ?? throw new InvalidOperationException("Не удалось получить данные из БД");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось выполнить запрос получения пользователей: {ex.Message}");
            }
        }

        // 🎯 ТЕПЕРЬ ТУТ ЧЕСТНЫЙ Guid вместо int
        public async Task<UserEntity?> GetByIdAsync(Guid id)
        {
            DbConnection connection = _context.Connection;
            string sql = _context.UserSqlGen.GenerateSelectById();

            return await connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { id = id });
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            DbConnection connection = _context.Connection;
            string sql = _context.UserSqlGen.GenerateSelectByEmail();

            return await connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { email = email });
        }

        public async Task UpdateAsync(UserEntity user)
        {
            DbConnection connection = _context.Connection;
            string sql = _context.UserSqlGen.GenerateUpdate();

            // Обновляем метку времени при любом редактировании
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await connection.ExecuteAsync(sql, user);
        }

        public async Task<bool> EmailExistsAsync(string? email)
        {
            try
            {
                // 1. Пробуем долбиться в текущую выбранную базу (Postgres)
                DbConnection connection = _context.Connection;
                var sql = _context.UserSqlGen.GenerateEmailExists();
                return await connection.ExecuteScalarAsync<bool>(sql, new { email });
            }
            catch (Exception ex) when (ex.Message.Contains("stream") || ex.Message.Contains("connection") || ex.InnerException?.Message.Contains("stream") == true)
            {
                // 🔥 МАГИЯ: Ловим падение сервера, пишем лог и переключаем рельсы на лету!
                System.Diagnostics.Debug.WriteLine("⚠️ Сервер Postgres не ответил! Автоматический прыжок на SQLite...");

                // Переключаем весь контекст приложения на локальный файл!
                await _context.SwitchDatabaseAsync(DatabaseType.SQLite);

                // 2. Повторяем этот же запрос еще раз, но уже в живую SQLite! 🔌⚡
                DbConnection connection = _context.Connection;
                var sql = _context.UserSqlGen.GenerateEmailExists();
                return await connection.ExecuteScalarAsync<bool>(sql, new { email });
            }
        }
    }
}
