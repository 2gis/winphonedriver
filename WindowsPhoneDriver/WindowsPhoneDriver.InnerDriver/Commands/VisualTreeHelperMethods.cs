namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    internal class VisualTreeHelperMethods
    {
        #region Methods

        internal static Point GetCoordinates(FrameworkElement element, UIElement visualRoot)
        {
            var point = element.TransformToVisual(visualRoot).Transform(new Point(0, 0));
            var center = new Point(point.X + (int)(element.ActualWidth / 2), point.Y + (int)(element.ActualHeight / 2));
            return center;
        }

        internal static IEnumerable<DependencyObject> GetDescendantsOfNameByPredicate(
            DependencyObject item, 
            string name)
        {
            return GetDescendantsByPredicate(item, x => ((FrameworkElement)x).Name.Equals(name));
        }

        internal static IEnumerable<DependencyObject> GetDescendantsOfTypeByPredicate(
            DependencyObject item, 
            string typeName)
        {
            return GetDescendantsByPredicate(item, x => x.GetType().ToString().Equals(typeName));
        }

        private static IEnumerable<DependencyObject> GetDescendantsByPredicate(
            DependencyObject rootItem, 
            Predicate<DependencyObject> predicate)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(rootItem);
            for (var i = 0; i < childrenCount; ++i)
            {
                var child = VisualTreeHelper.GetChild(rootItem, i);
                if (predicate(child))
                {
                    yield return child;
                }

                foreach (var grandChild in GetDescendantsByPredicate(child, predicate))
                {
                    yield return grandChild;
                }
            }
        }

        #endregion
    }
}
