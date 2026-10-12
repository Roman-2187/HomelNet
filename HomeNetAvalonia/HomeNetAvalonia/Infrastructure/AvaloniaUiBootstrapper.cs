using Dapper;
using HomeNet.DI;
using HomeNetAvalonia.Animators;
using HomeNetCore.Enums;
using HomeNetOrm.Helpers;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        // Теперь возвращаем наш чистый ручной корень композиции, а не IServiceProvider!
        public static AppCompositionRoot BuildAvaloniaContainer(BackendMode mode, string postgresConn, string sqliteConn)
        {
            // 🔨 Обучаем Dapper читать DateTimeOffset из строк SQLite
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());


          

            // 🔥 ВОТ ЭТА СТРОЧКА СПАСЕТ СИТУАЦИЮ:
            // Обучаем Dapper парсить текстовые Guid из колонок SQLite в нормальные Guid-объекты C#
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // 🚀 Просто инициализируем наш ручной конструктор на "new".
            // Внутри него уже создадутся все 4 модуля, репозитории, вьюмодели и навигация!
            AppCompositionRoot root = AppCompositionRoot.Initialize(mode, postgresConn, sqliteConn);

            // Если нужен аниматор окон для реального режима, можем создать его прямо тут 
            // или положить свойством в PresentationUiModule.
            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                // Если CloseWindowAnimator всё еще нужен как синглтон, 
                // пускай просто создаётся в лоб или используется как статика
                var animator = new CloseWindowAnimator(root.EventBus);
            }

            return root;
        }
    }
}
