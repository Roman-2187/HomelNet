using Dapper;
using HomeNet.DI;
using HomeNetAvalonia.Animators;
using HomeNetCore.Enums;
using HomeNetOrm.Helpers;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        // Переименовали postgresConn -> apiBaseUrl для чистоты архитектуры
        public static AppCompositionRoot BuildAvaloniaContainer(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            // 🔨 Обучаем Dapper читать DateTimeOffset из строк SQLite
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());

            // Обучаем Dapper парсить текстовые Guid из колонок SQLite в нормальные Guid-объекты C#
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // 🚀 1. Запускаем инициализацию корня композиции
            AppCompositionRoot.Initialize(mode, apiBaseUrl, sqliteConn);

            // 🚀 2. Вытаскиваем готовый экземпляр через статическое свойство (предполагаем, что оно есть в AppCompositionRoot)
            AppCompositionRoot root = AppCompositionRoot.Instance;

            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                var animator = new CloseWindowAnimator(root.EventBus);
            }

            return root;
        }
    }
}
