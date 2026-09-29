using System;

namespace HomeNetCore.Models.Diagnostics
{
    /// <summary>
    /// Битовый автомат состояний сигнала в шине SiberNet.
    /// Исключает парсинг строк и циклы сравнения коллекций.
    /// </summary>
    [Flags]
    public enum MessageState
    {
        None = 0,

        // 🟢 Прошла публикация (Бит 1)
        Published = 1,

        // 🟡 Прошла подписка (Бит 2)
        Subscribed = 2,

        // 🔴 И то, и другое одновременно! (Бит 1 + Бит 2)
        Live = Published | Subscribed
    }
}
