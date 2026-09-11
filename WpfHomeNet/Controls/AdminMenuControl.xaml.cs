using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WpfHomeNet.UiHelpers;

namespace WpfHomeNet.Controls
{
    public partial class AdminMenu : UserControl
    {
        public AdminMenu()
        {
            InitializeComponent();
            this.Loaded += AdminMenu_Loaded;
        }

        private void AdminMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var app = (HomeSocialNetwork.App)Application.Current;
            var queueManager = app.Services.GetRequiredService<LogQueueManager>();

            queueManager.OnLogReceived += AdminLogRender.AddLog;
        }

        // ⚡️ ВОЗВРАЩАЕМ МЕТОД ДЛЯ СНЯТИЯ СБОЯ XAML-ПАРСЕРА!
        private void DeleteUsers_Loaded(object sender, RoutedEventArgs e)
        {
            // Можно оставить пустым, он нужен просто чтобы разметка не падала
        }
    }
}
