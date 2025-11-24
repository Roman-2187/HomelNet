using HomeNetCore.Helpers;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfHomeNet.UiHelpers
{

    // Новый класс для рендеринга логов
    public class LogRenderer : ILogRenderer
    {
        private readonly RichTextBox _logTextBox;
        private readonly Dictionary<LogColor, Brush> _colorMap = new()
    {
        { LogColor.Critical, Brushes.Red },
        { LogColor.Error, Brushes.OrangeRed },
        { LogColor.Warning, Brushes.Orange },
        { LogColor.Information, Brushes.Green },
        { LogColor.Debug, Brushes.Tan },
        { LogColor.Trace, Brushes.Gray }
    };

        public LogRenderer(RichTextBox logTextBox)
        {
            _logTextBox = logTextBox;
        }

        public async Task AddLog(string text, LogLevel level, LogColor color, bool isAnimating)
        {
            await Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
            {
                try
                {
                    if (_logTextBox.Document == null)
                    {
                        _logTextBox.Document = new FlowDocument();
                    }

                    if (isAnimating)
                    {
                        var lastParagraph = GetLastParagraph(_logTextBox.Document);

                        if (lastParagraph == null)
                        {
                            lastParagraph = new Paragraph();
                            _logTextBox.Document.Blocks.Add(lastParagraph);
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

                    _logTextBox.ScrollToEnd();
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
            if (_logTextBox.Document == null)
            {
                _logTextBox.Document = new FlowDocument();
            }

            var paragraph = new Paragraph();

            var run = new Run(text)
            {
                Foreground = _colorMap.ContainsKey(color) ? _colorMap[color] : Brushes.White
            };

            paragraph.Inlines.Add(run);
            _logTextBox.Document.Blocks.Add(paragraph);
        }
    }





}
