using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace HomeNetCore.Interfaces
{
    /// <summary>
    /// Глобальный контракт для Главной ВьюМодели. 
    /// Сюда выносим всё, что UI и аниматоры должны видеть в MainWindow.
    /// </summary>
    public interface IMainViewModel : INotifyPropertyChanged
    {
        // Наш тумблер видимости логов, из-за которого сыпалась ошибка
        bool IsGlobalLoggerVisible { get; set; }

        // Сюда же на будущее можно накидать остальные общие свойства, если понадобятся:
        // MainTab CurrentMainTab { get; set; }
    }
}
