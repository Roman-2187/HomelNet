using System.Windows;

namespace WpfHomeNet.Helpers
{
    public static class FlyOutBridge
    {
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.RegisterAttached(
                "IsOpen",
                typeof(bool),
                typeof(FlyOutBridge),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static bool GetIsOpen(DependencyObject obj) => (bool)obj.GetValue(IsOpenProperty);
        public static void SetIsOpen(DependencyObject obj, bool value) => obj.SetValue(IsOpenProperty, value);
    }
}
