using System;
using System.Windows;
using System.Windows.Media.Animation;
using HomeNetCore.Interfaces.Events;
using HomeNetCore.Interfaces.ViewModels;
using SiberNet.UI.Infrastructure.Base;

namespace SiberNet.UI.Infrastructure.Animators
{
    public class ResizeWindowAnimator : BaseWindowAnimation
    {
        private readonly IEventBus _eventBus;
        private bool _isMaximized = false; //
        private double _storedWidth = 900; //
        private double _storedHeight = 600; //

        public ResizeWindowAnimator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventBus.Subscribe<IMainViewModel.ToggleSize>(OnToggleWindowSizeRequested); //
        }

        private void OnToggleWindowSizeRequested(IMainViewModel.ToggleSize msg)
        {
            ExecuteOnUi(window =>
            {
                double screenW = SystemParameters.PrimaryScreenWidth; //
                double screenH = SystemParameters.PrimaryScreenHeight; //
                double targetWidth, targetHeight, targetTop, targetLeft;

                if (!_isMaximized) //
                {
                    _storedWidth = window.Width; //
                    _storedHeight = window.Height; //

                    targetWidth = screenW; //
                    targetHeight = screenH; //
                    targetTop = 0; //
                    targetLeft = 0; //
                    _isMaximized = true; //
                }
                else
                {
                    targetWidth = _storedWidth; //
                    targetHeight = _storedHeight; //
                    targetTop = (screenH - targetHeight) / 2; //
                    targetLeft = (screenW - targetWidth) / 2; //
                    _isMaximized = false; //
                }

                var ease = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 6 }; //
                var duration = TimeSpan.FromSeconds(0.85); //

                window.BeginAnimation(Window.WidthProperty, new DoubleAnimation(window.Width, targetWidth, duration) { EasingFunction = ease }); //
                window.BeginAnimation(Window.HeightProperty, new DoubleAnimation(window.Height, targetHeight, duration) { EasingFunction = ease }); //
                window.BeginAnimation(Window.TopProperty, new DoubleAnimation(window.Top, targetTop, duration) { EasingFunction = ease }); //
                window.BeginAnimation(Window.LeftProperty, new DoubleAnimation(window.Left, targetLeft, duration) { EasingFunction = ease }); //
            });
        }

        public override void Dispose()
        {
            _eventBus.Unsubscribe<IMainViewModel.ToggleSize>(OnToggleWindowSizeRequested); //
        }
    }
}
