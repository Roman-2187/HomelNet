using HomeNetCore.Helpers;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;



namespace WpfHomeNet
{

    public partial class LogWindow : Window
    {
        // Обновляем маппинг цветов
        private Dictionary<LogColor, Brush> _colorMap = new()
    {
        { LogColor.Critical, Brushes.Red },
        { LogColor.Error, Brushes.OrangeRed },
        { LogColor.Warning, Brushes.Orange },
        { LogColor.Information, Brushes.Green },
        { LogColor.Debug, Brushes.Blue },
        { LogColor.Trace, Brushes.Gray }
    };

        private bool _isFirstMessage = true; // Флаг для первого сообщения

        public LogWindow()
        {
            InitializeComponent();
        }

        // Обновляем сигнатуру метода AddLog
        public async Task AddLog(string text, LogLevel level, LogColor color, bool isAnimating)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    if (LogTextBox.Document == null)
                    {
                        LogTextBox.Document = new FlowDocument();
                    }

                    if (isAnimating)
                    {
                        var lastParagraph = GetLastParagraph(LogTextBox.Document);

                        if (lastParagraph == null)
                        {
                            lastParagraph = new Paragraph();
                            LogTextBox.Document.Blocks.Add(lastParagraph);
                        }

                        lastParagraph.Inlines.Clear();
                        var run = new Run(text)
                        {
                            Foreground = _colorMap.ContainsKey(color) ? _colorMap[color] : Brushes.White
                        };
                        lastParagraph.Inlines.Add(run);
                    }
                    else
                    {
                        AddNewLine(text, color);
                    }

                    LogTextBox.ScrollToEnd();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при обновлении лога: {ex.Message}");
                }
            }, DispatcherPriority.Normal);
        }

        private Paragraph? GetLastParagraph(FlowDocument document)
        {
            if (document?.Blocks == null || document.Blocks.Count == 0)
                return null;
            return document.Blocks.LastBlock as Paragraph;
        }

        private void AddNewLine(string text, LogColor color)
        {
            if (LogTextBox.Document == null)
            {
                LogTextBox.Document = new FlowDocument();
            }

            var paragraph = new Paragraph();

            var run = new Run(text)
            {
                Foreground = _colorMap.ContainsKey(color) ? _colorMap[color] : Brushes.White
            };

            paragraph.Inlines.Add(run);
            LogTextBox.Document.Blocks.Add(paragraph);
        }
    }








}