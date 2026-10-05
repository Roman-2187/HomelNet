using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using HomeNetAvalonia;
using System.Runtime.InteropServices.JavaScript; // Подключаем Interop для работы с JS

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            // 🔥 КИБЕР-ВНЕДРЕНИЕ ДЛЯ ХРОМА И ЯНДЕКСА НА .NET 10 WASM:
            // Прямо из C# выполняем сырой JS-код, который создаёт объект 
            // AvaloniaDOM и вешает туда нужный метод getGlobalThis.
            // Теперь движок Авалонии не споткнётся об undefined!
            JSHost.ImportAsync("inline_fix", "data:text/javascript,window.AvaloniaDOM = window.AvaloniaDOM || {}; window.AvaloniaDOM.getGlobalThis = function() { return globalThis || window || self; };").GetAwaiter().GetResult();
        }
        catch { /* Если рантайтм ещё прогревается, пропускаем */ }

        try
        {
            // Наша микропауза для подстраховки нативной графики Skia
            await Task.Delay(300);
            _ = SkiaSharp.SKImageInfo.Empty;
        }
        catch { }

        // Стартуем асинхронный запуск веб-версии!
        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>().UseHarfBuzz(); 
}

