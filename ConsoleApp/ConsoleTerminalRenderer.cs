using System;
using HomeNetCore.Enums;
using HomeNetPresentation.ViewModels.AdminViews;

namespace SiberNet.ConsoleTest
{
    public static class ConsoleTerminalRenderer
    {
        /// <summary>
        /// 🔥 РЕНДЕРЕР МАТРИЦЫ В СИСТЕМНУЮ КОНСОЛЬ
        /// </summary>
        public static void PrintToConsole(TerminalLogsViewModel viewModel)
        {
            // Блокируем экран, чтобы не было мерцания при обновлении
            Console.Clear();
            Console.CursorVisible = false;

            // 1. Бежим по строкам нашего двухмерного массива
            foreach (var line in viewModel.Logs)
            {
                // 2. Бежим по символам внутри текущей строки
                foreach (var logChar in line)
                {
                    // Переключаем цвет консоли перед выводом КАЖДОЙ буквы
                    Console.ForegroundColor = MapLogLevelToConsoleColor(logChar.Level);

                    // Печатаем символ горизонтально в один ряд
                    Console.Write(logChar.Value);
                }

                // В конце строки сбрасываем цвет в дефолтный и прыгаем на строку ниже
                Console.ResetColor();
                Console.WriteLine();
            }
        }




        /// <summary>
        /// Тот самый свитч уровней, только для системной консоли
        /// </summary>
        private static ConsoleColor MapLogLevelToConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Critical => ConsoleColor.Red,         // Крит — красный
                LogLevel.Error => ConsoleColor.DarkRed,     // Ошибка — темно-красный
                LogLevel.Warning => ConsoleColor.Yellow,      // Ворнинг — желтый
                LogLevel.Debug => ConsoleColor.DarkGray,    // Дебаг — серый
                _ => ConsoleColor.Green        // Твой любимый Info — зеленый
            };
        }
    }
}
