using Dapper;
using HomeNet.DI;
using HomeNet.Network.SignalRListeners;
using HomeNetAvalonia.Animators;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetOrm.Helpers;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.DbInfrastructure;
using System;
using System.Threading.Tasks;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        private static bool _isInitialized = false;
        private static bool _isSyncStarted = false;

        public static AppCompositionRoot BuildAvaloniaContainer(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            if (_isInitialized) return AppCompositionRoot.Instance;

            // 1. Базовые мапперы типов Dapper
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // 2. Строим каркас
            AppCompositionRoot.Initialize(mode, apiBaseUrl, sqliteConn);
            _isInitialized = true;

            AppCompositionRoot root = AppCompositionRoot.Instance;

           

            if (mode == BackendMode.Real || mode == BackendMode.Local)
            {
                _ = new CloseWindowAnimator(root.EventBus);
                _ = new MinimizeWindowAnimator(root.EventBus);
                _ = new WidescreenWindowAnimator(root.EventBus);
            }

            // ============================================================================
            // 💥 СТАРТ ИНИЦИАЛИЗАЦИИ БД
            // ============================================================================
            Task.Run(async () =>
            {
                await root.DataAccess.DbContextContainer.InitializeAllDatabasesAsync();
            });

            return root;
        }

        

       

        
    }
}