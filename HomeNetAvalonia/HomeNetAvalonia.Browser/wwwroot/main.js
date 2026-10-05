// 🔥 КИБЕР-ЗАПЛАТКА ДЛЯ КРОСС-БРАУЗЕРНОГО СТАРТА .NET 10
// Насильно объявляем объект и метод getGlobalThis в контексте окна,
// чтобы Авалония не падала в глухой undefined при инициализации DOM!
if (typeof window !== "undefined") {
    window.AvaloniaDOM = window.AvaloniaDOM || {};
    window.AvaloniaDOM.getGlobalThis = function () {
        return globalThis || window || self;
    };
}

import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
