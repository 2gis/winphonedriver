namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System.Collections.Generic;

    using WindowsPhoneDriver.Common;

    internal class GetWindowSizeExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            var phoneScreenSize = this.Automator.EmulatorController.PhoneScreenSize;

            return this.JsonResponse(
                ResponseStatus.Success, 
                new Dictionary<string, int> { { "width", phoneScreenSize.Width }, { "height", phoneScreenSize.Height } });
        }

        #endregion
    }
}
