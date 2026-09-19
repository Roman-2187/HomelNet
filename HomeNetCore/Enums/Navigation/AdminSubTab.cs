using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetCore.Enums.Navigation
{
    /// <summary>
    /// Макро-рубильник для переключения ГЛАВНЫХ панелей внутри админки.
    /// </summary>
    public enum AdminSubTab
    {
        None,
        UserTable,        // Таблица пользователей
        DeleteUserForm,   // Форма удаления
        LogPanel,         // Панель логов (Родитель для фильтров!)
        EventInspector    // Инспектор событий бэкенда
    }
}



