using System;
using System.Collections.Generic;
using HomeNet.Infrastructure.Shared.Enums;
using HomeNetCore.Models.Diagnostics; // Наш "паспортный стол" узлов

namespace HomeNet.Infrastructure.Shared.Interfaces.Diagnostics
{
    /// <summary>
    /// Контракт тотального рантайм-аудита и профилирования потока данных внутри EventBus.
    /// </summary>
    public interface IEventInspector
    {
        /// <summary>
        /// Элемент строки отчета для зрячего архитектурного графа
        /// </summary>
        public record Line(string Text, MessageState State);

        /// <summary> Фиксация и валидация факта публикации сигнала в автобусе </summary>
        void RecordPublish(object sender, Type messageType);

        /// <summary> Регистрация новой подписки и склеивание стрелочек графа зависимостей </summary>
        void RecordSubscribe(string componentName, Type messageType, string methodName, Type subscriberType);

        /// <summary> Корректное гашение флагов активности при отписке компонента </summary>
        void RecordUnsubscribe(Type messageType);

        /// <summary> Сборка актуального объектного графа системы </summary>
        List<Line> GenerateObjectGraph();

        /// <summary> Вывод готового текстового или HTML отчета для сброса на десктоп </summary>
        string GenerateReport();
    }
}
