namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    internal class QuitExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            this.Automator.Deployer.Disconnect();

            return this.JsonResponse();
        }

        #endregion
    }
}
