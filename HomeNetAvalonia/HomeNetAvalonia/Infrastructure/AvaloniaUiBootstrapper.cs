using Dapper;
using HomeNet.DI;
using HomeNetAvalonia.Animators;
using HomeNetCore.Enums;
using HomeNetOrm.Helpers;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        // 🔥 СТАТИЧЕСКИЙ ФЛАГ-ПРЕДОХРАНИТЕЛЯ ТЕПЕРЬ ЖИВЕТ ТУТ
        private static bool _isInitialized = false;

        public static AppCompositionRoot BuildAvaloniaContainer(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            // Если контейнер уже был успешно собран (например, при первом проходе),
            // мы просто молча возвращаем готовый экземпляр и не устраиваем каскадный краш СУБД!
            if (_isInitialized)
            {
                return AppCompositionRoot.Instance;
            }

            // 🔨 Обучаем Dapper читать DateTimeOffset из строк SQLite
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());

            // Обучаем Dapper парсить текстовые Guid из колонок SQLite
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // 🚀 1. Запускаем инициализацию корня композиции (строго ОДИН раз за жизнь приложения!)
            AppCompositionRoot.Initialize(mode, apiBaseUrl, sqliteConn);

            // Взводим флаг, блокируя повторные "холостые" выстрелы Avalonia
            _isInitialized = true;

            // 🚀 2. Вытаскиваем готовый экземпляр через статическое свойство
            AppCompositionRoot root = AppCompositionRoot.Instance;

            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                // Старый аниматор закрытия окна
                var animator = new CloseWindowAnimator(root.EventBus);

                // 🔥 НАШ НОВЫЙ АНИМАТОР ШИРОКОФОРМАТНОГО РЕЖИМА
                // Теперь он просыпается прямо при старте UI-слоя Авалонии!
                var widescreenAnimator = new WidescreenWindowAnimator(root.EventBus);
            }

            return root;
        }
    }
}
