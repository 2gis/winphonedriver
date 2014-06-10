using System.Windows;
using System.Windows.Media;

namespace WindowsPhoneJsonWireServer
{
    public static class UiHelpers
    {
        // Temporary basic implimentation. Does not check if view out of bounds or covered by other view
        public static bool IsUserVisible(this FrameworkElement element)
        {
            while (true)
            {
                if (element.Visibility != Visibility.Visible || !element.IsHitTestVisible)
                    return false;

                var container = VisualTreeHelper.GetParent(element) as FrameworkElement;

                if (container == null) return true;

                element = container;
            }
        }
    }
}
