using System.Diagnostics;
using System.IO;

namespace HomeNetOrm.Helpers
{
    public static class DatabasePathHelper
    {
        // Базовый путь к папке с .exe
        private static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        // Фиксированная папка для БД
        private static readonly string DbFolder = Path.Combine(BasePath, "BD");

        public static string GetDatabasePath(string dbFileName)
        {
            // Создаем папку, если её нет
            Directory.CreateDirectory(DbFolder);

            // Формируем полный путь
            string fullPath = Path.Combine(DbFolder, dbFileName);

            // Создаем файл, если его нет
            if (!File.Exists(fullPath))
            {
                File.Create(fullPath).Dispose();
            }

            return fullPath;
        }
    }


        class TestMain
        {
          string connectionString = $"Data Source={DatabasePathHelper.GetDatabasePath("home_net.db")}";
        }



}


