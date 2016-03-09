namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    #region

    using System.Collections.Generic;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.OuterDriver.CommandExecutors.CommandHelpers;

    #endregion

    internal class StatusExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            var response = new Dictionary<string, object> { { "build", new BuildInfo() }, };
            return this.JsonResponse(ResponseStatus.Success, response);
        }

        #endregion
    }
}