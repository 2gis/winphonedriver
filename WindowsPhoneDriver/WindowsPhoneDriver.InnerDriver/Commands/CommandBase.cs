namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System;
    using System.Collections.Generic;

    using WindowsPhoneDriver.Common;

    internal class CommandBase
    {
        #region Public Properties

        public Automator Automator { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        #endregion

        #region Public Methods and Operators

        public string Do()
        {
            if (this.Automator == null)
            {
                throw new InvalidOperationException("Automator must be set before Do() is called");
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
