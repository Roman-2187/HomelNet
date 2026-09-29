using HomeNetCore.Models.Diagnostics;

namespace HomeNetCore.Interfaces.Diagnostics
{
    public interface IEventInspector
    {
        string GenerateReport();


        // 🔥 ВЛОЖЕННЫЙ РЕКОРД: Прямо внутри контракта!
        // Теперь каждый, кто видит интерфейс, автоматически видит и структуру его строки.
        public record Line(string Text, MessageState State);

        // Возвращаем список НАШИХ вложенных рекордов
        List<Line> GenerateObjectGraph();


    }

}