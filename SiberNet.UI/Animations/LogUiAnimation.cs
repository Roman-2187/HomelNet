using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.OutputLogging;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel; // Для защиты дизайна VS
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace SiberNet.UI.Animations
{
    public static class LogUiAnimation
    {
        // 🎨 ТВОЯ ОРИГИНАЛЬНАЯ КАРТА ЦВЕТОВ С ГИТХАБА:
        private static readonly Dictionary<LogLevel, Brush> _colorMap = new()
        {
            { LogLevel.Error, Brushes.OrangeRed },
            { LogLevel.Critical, Brushes.OrangeRed },
            { LogLevel.Warning, Brushes.Orange },
            { LogLevel.Information, Brushes.LightGreen },
            { LogLevel.Debug, Brushes.Tan }
        };

        public static readonly DependencyProperty RegisterBridgeProperty =
            DependencyProperty.RegisterAttached(
                "RegisterBridge",
                typeof(bool),
                typeof(LogUiAnimation),
                new PropertyMetadata(false, OnRegisterBridgeChanged));

        public static void SetRegisterBridge(DependencyObject element, bool value)
            => element.SetValue(RegisterBridgeProperty, value);

        public static bool GetRegisterBridge(DependencyObject element)
            => (bool)element.GetValue(RegisterBridgeProperty);

        private static void OnRegisterBridgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(d)) return;

            if (d is RichTextBox logTextBox && (bool)e.NewValue)
            {
                logTextBox.Document ??= new FlowDocument();
                logTextBox.Document.Blocks.Clear();

                // 1. Вытаскиваем шину событий из нашего DI провайдера
                var provider = HomeNet.DI.AppBootstrapper.ServiceProvider;
                var eventBus = provider.GetRequiredService<IEventBus>();

                // 2. 🔥 ЛОВИМ СИГНАЛЫ ИЗ АВТОБУСА И СКОРМЛИВАЕМ ИХ В ТВОЙ МЕТОД ADDLOG:
                eventBus.Subscribe<ILogQueueManager.LogMessageReceived>(async msg =>
                {
                    // Разбираем рекорд шины событий на параметры
                    await AddLog(logTextBox, msg.Text, msg.Level, msg.IsAnimating);
                });
            }
        }

        // 🚀 ТВОЙ ОРИГИНАЛЬНЫЙ МЕТОД ADDLOG (Состыкован под IEventBus и приоритет Background):
        // 🚀 ТЕПЕРЬ ОН ОЖИВЕТ ПРИ ЛЮБЫХ ПОТОКАХ И АВТОБУСАХ!
        public static async Task AddLog(RichTextBox logTextBox, string text, LogLevel level, bool isAnimating)
        {
            // 🔥 ИСПРАВЛЕНИЕ ВЕКА: Заменили Dispatcher.CurrentDispatcher на logTextBox.Dispatcher!
            // Теперь WPF жестко перенаправит посимвольный поток букв из автобуса событий 
            // прямо в главный графический поток интерфейса, и экран мгновенно проснется!
            await logTextBox.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    if (logTextBox.Document == null)
                    {
                        logTextBox.Document = new FlowDocument();
                    }

                    if (isAnimating)
                    {
                        var lastParagraph = GetLastParagraph(logTextBox.Document);

                        if (lastParagraph == null)
                        {
                            lastParagraph = new Paragraph();
                            logTextBox.Document.Blocks.Add(lastParagraph);
                        }

                        lastParagraph.Inlines.Clear();
                        var run = new Run(text)
                        {
                            Foreground = _colorMap.ContainsKey(level) ? _colorMap[level] : Brushes.White
                        };
                        lastParagraph.Inlines.Add(run);
                    }
                    else
                    {
                        AddNewLine(logTextBox, text, level);
                    }

                    // Твой оригинальный автоскролл через PART_ContentHost
                    logTextBox.ScrollToEnd();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при обновлении лога: {ex.Message}");
                }
                await Task.CompletedTask;
            }, DispatcherPriority.Background); // Держим Background, чтобы "Загрузка системы" не висла
        }

        private static Paragraph? GetLastParagraph(FlowDocument document)
        {
            if (document?.Blocks == null || document.Blocks.Count == 0)
                return null;
            return document.Blocks.LastBlock as Paragraph;
        }

        private static void AddNewLine(RichTextBox logTextBox, string text, LogLevel level)
        {
            if (logTextBox.Document == null)
            {
                logTextBox.Document = new FlowDocument();
            }

            var paragraph = new Paragraph();
            var run = new Run(text)
            {
                Foreground = _colorMap.ContainsKey(level) ? _colorMap[level] : Brushes.White
            };

            paragraph.Inlines.Add(run);
            logTextBox.Document.Blocks.Add(paragraph);
        }
    }
}
