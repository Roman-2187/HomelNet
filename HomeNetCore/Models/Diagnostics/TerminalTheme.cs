using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetCore.Models.Diagnostics
{
    namespace SiberNet.Core.Diagnostics
    {
        public record TerminalTheme(
            string Critical = "#FF3333", // Ярко-красный
            string Error = "#FF7575", // Красный
            string Warning = "#FFCC66", // Оранжевый/Желтый
            string Debug = "#B5CEA8", // Зеленоватый/Серый
            string Info = "#90EE90", // LightGreen
            string Default = "#808080"  // Серый для разделителей
        );
    }

}
