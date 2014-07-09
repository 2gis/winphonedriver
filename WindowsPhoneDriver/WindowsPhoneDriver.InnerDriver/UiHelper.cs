namespace WindowsPhoneDriver.InnerDriver
{
    using System;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;

    public static class UiHelpers
    {
        // Temporary basic implementation. Does not check if view out of bounds or covered by other view
        #region Public Methods and Operators

        public static void BeginInvokeSync(Action action)
        {
            Exception exception = null;
            var waitEvent = new AutoResetEvent(false);

            Deployment.Current.Dispatcher.BeginInvoke(
                () =>
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }

                        waitEvent.Set();
                    });
            waitEvent.WaitOne();

            if (exception != null)
            {
                throw exception;
            }
        }

        public static bool IsUserVisible(this FrameworkElement element)
        {
            while (true)
            {
                if (element.Visibility != Visibility.Visible || !element.IsHitTestVisible)
                {
                    return false;
                }

                var container = VisualTreeHelper.GetParent(element) as FrameworkElement;
                if (container == null)
                {
                    return true;
                }

                element = container;
            }
        }

        #endregion
    }
}
