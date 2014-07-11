namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System.Windows;

    internal class VisualTreeHelperMethods
    {
        #region Methods

        internal static Point GetCoordinates(FrameworkElement element, UIElement visualRoot)
        {
            var point = element.TransformToVisual(visualRoot).Transform(new Point(0, 0));
            var center = new Point(point.X + (int)(element.ActualWidth / 2), point.Y + (int)(element.ActualHeight / 2));
            return center;
        }

        #endregion
    }
}
