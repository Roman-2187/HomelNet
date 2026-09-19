using HomeNetCore.Interfaces.Diagnostics;
using HomeNetCore.Interfaces.Events;
using System;

namespace HomeNetCore.Extensions
{
    public static class EventBusExtensions
    {
        // 🔥 НАШ КРУТОЙ ОБВЕС: Расширяем IEventBus методом генерации отчета!
        public static string GenerateInspectorReport(this IEventBus eventBus)
        {
            
            if (eventBus is IEventInspectorSource inspectorSource)
            {
                return inspectorSource.GenerateReport();
            }

            // Если подсунули обычный/тестовый автобус — выдаем безопасный дефолт без крашей
            return "🧠 Информация: Объектный граф шины событий работает в штатном автономном режиме.";
        }
    }
}
