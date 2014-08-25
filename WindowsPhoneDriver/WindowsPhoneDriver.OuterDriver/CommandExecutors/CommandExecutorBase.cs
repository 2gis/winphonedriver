namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System;

    using Newtonsoft.Json;

    using OpenQA.Selenium.Remote;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.OuterDriver.Automator;

    internal class CommandExecutorBase
    {
        #region Public Properties

        public Command ExecutedCommand { get; set; }

        #endregion

        #region Properties

        protected Automator Automator { get; set; }

        #endregion

        #region Public Methods and Operators

        public string Do()
        {
            if (this.ExecutedCommand == null)
            {
                throw new NullReferenceException("ExecutedCommand property must be set before calling Do");
            }

            try
            {
                var session = this.ExecutedCommand.SessionId == null ? null : this.ExecutedCommand.SessionId.ToString();
                this.Automator = Automator.InstanceForSession(session);
                return this.DoImpl();
            }
            catch (AutomationException ex)
            {
                return this.JsonResponse(ex.Status, ex.Message);
            }
        }

        #endregion

        #region Methods

        protected virtual string DoImpl()
        {
            throw new InvalidOperationException("DoImpl should never be called in CommandExecutorBase");
        }

        protected string JsonResponse(ResponseStatus status, object value)
        {
            // TODO: there are no support for sessions at the moment
            const string Session = "awesomeSession";

            return JsonConvert.SerializeObject(new JsonResponse(Session, status, value));
        }

        #endregion
    }
}
