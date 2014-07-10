namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    using WindowsPhoneDriver.Common;

    internal class CommandBase
    {
        #region Public Properties

        public UIElement VisualRoot { get; set; }

        public Dictionary<string, FrameworkElement> WebElements { get; set; }

        #endregion

        #region Public Methods and Operators

        public string Do()
        {
            if (this.WebElements == null)
            {
                throw new InvalidOperationException("WebElements must be set before Do() is called");
            }

            if (this.VisualRoot == null)
            {
                throw new InvalidOperationException("VisualRoot must be set before Do() is called");
            }

            var response = string.Empty;
            try
            {
                UiHelpers.BeginInvokeSync(() => { response = this.DoImpl(); });
            }
            catch (AutomationException exception)
            {
                response = Responder.CreateJsonResponse(exception.Status, exception.Message);
            }

            return response;
        }

        public virtual string DoImpl()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
