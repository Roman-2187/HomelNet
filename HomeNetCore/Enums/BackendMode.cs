namespace HomeNetCore.Enums
{
    /// <summary>
    /// Режим работы бэкенда приложения.
    /// </summary>
    public enum BackendMode
    {
        /// <summary> Реальная работа с живыми базами данных (PostgreSQL / SQLite). </summary>
        Real,

        /// <summary> Режим пустышки/заглушки (для тестов, консоли или демо-режима без БД). </summary>
        Fake,
        Local
    }
}
