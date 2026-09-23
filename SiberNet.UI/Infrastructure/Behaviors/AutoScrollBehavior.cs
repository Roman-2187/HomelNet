using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors; 

namespace SiberNet.UI.Infrastructure.Behaviors
{
    public class AutoScrollBehavior : Behavior<ScrollViewer>
    {
        private Panel? _innerPanel;

        protected override void OnAttached()
        {
            base.OnAttached();
            // Подписываемся на событие загрузки ScrollViewer в визуальное дерево
            AssociatedObject.Loaded += OnScrollViewerLoaded;
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Loaded -= OnScrollViewerLoaded;
            }
            if (_innerPanel != null)
            {
                _innerPanel.SizeChanged -= OnPanelSizeChanged;
            }
            base.OnDetaching();
        }

        private void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
        {
            // Ищем внутренний StackPanel, который лежит внутри ScrollViewer
            if (AssociatedObject.Content is Panel panel)
            {
                _innerPanel = panel;
                // 🔥 Как только панель меняет свой размер (дописались буквы или упала новая строка) — скроллим вниз!
                _innerPanel.SizeChanged += OnPanelSizeChanged;
            }
        }

        private void OnPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Мёртвый автоскролл до упора
            AssociatedObject?.ScrollToEnd();
        }
    }
}
