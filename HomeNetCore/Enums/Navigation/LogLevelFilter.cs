using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetCore.Enums.Navigation
{
    /// <summary>
    /// Микро-рубильник для фильтрации логов внутри LogPanel.
    /// </summary>
    public enum LogLevelFilter
    {
        All,        // Показать вообще всё
        Warning,    // Только оранжевые варнинги
        Error,      // Только красные ошибки
        Critical    // Только критические сбои базы данных
    }
}
