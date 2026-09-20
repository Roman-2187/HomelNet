using System;
using System.Windows;
using System.Windows.Media.Animation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using SiberNet.UI.Infrastructure.Base;

namespace SiberNet.UI.Infrastructure.Animators
{
    public class CloseWindowAnimator : BaseWindowAnimation
    {
        private readonly IEventBus _eventBus;

        public CloseWindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventBus.Subscribe<IMainViewModel.CloseRequest>(OnWindowCloseRequested); //
        }

        private void OnWindowCloseRequested(IMainViewModel.CloseRequest msg)
        {
            ExecuteOnUi(window =>
            {
                double screenHeight = SystemParameters.PrimaryScreenHeight; //
                var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn }; //

                var exitAnimation = new DoubleAnimation
                {
                    To = screenHeight + 150, //
                    Duration = TimeSpan.FromMilliseconds(500), //
                    EasingFunction = easeIn //
                };

                exitAnimation.Completed += (s, args) => Application.Current.Shutdown(); //
                window.BeginAnimation(Window.TopProperty, exitAnimation); //
            });
        }

        public override void Dispose()
        {
            _eventBus.Unsubscribe<IMainViewModel.CloseRequest>(OnWindowCloseRequested); //
        }
    }
}
