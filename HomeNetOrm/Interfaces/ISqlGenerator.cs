using HomeNetOrm.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetOrm.Interfaces
{

    /// <summary>
    /// Атомарный контракт универсального генератора SQL.
    /// Содержит только чистый базовый CRUD.
    /// </summary>
    public interface ISqlGenerator<T> where T : class
    {
        string GenerateInsert();
        string GenerateUpdate();
        string GenerateDelete();
        string GenerateSelectById();
        string GenerateSelectAll();

        /// <summary>
        /// Метаданные адаптированной под snake_case схемы таблицы.
        /// Используются внешними статическими методами расширения (Extensions) для сборки кастомных SQL.
        /// </summary>
        TableSchema FormattedTable { get; }
    }
}
