namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows;

    using WindowsPhoneDriver.Common;

    internal class CommandBase
    {
        #region Public Properties

        public Automator Automator { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        #endregion

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

        public string Do()
        {
            if (this.Automator == null)
            {
                throw new InvalidOperationException("Automator must be set before Do() is called");
            }

            var response = string.Empty;
            try
            {
                BeginInvokeSync(() => { response = this.DoImpl(); });
            }
            catch (AutomationException exception)
            {
                response = Responder.CreateJsonResponse(exception.Status, exception.Message);
            }
            catch (Exception exception)
            {
                response = Responder.CreateJsonResponse(ResponseStatus.UnknownError, "Unknown error: " + exception.Message);
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
