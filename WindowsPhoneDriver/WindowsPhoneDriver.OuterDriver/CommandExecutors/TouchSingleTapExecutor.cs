namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using WindowsPhoneDriver.OuterDriver.Automator;
    using WindowsPhoneDriver.OuterDriver.EmulatorHelpers;

    internal class TouchSingleTapExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            this.Automator.UpdatedOrientationForEmulatorController();

            var elementId = Automator.GetValue<string>(this.ExecutedCommand.Parameters, "element");
            if (elementId == null)
            {
                return this.JsonResponse();
            }

            var tapPoint = this.Automator.RequestElementLocation(elementId).GetValueOrDefault();
            this.Automator.EmulatorController.PerformGesture(new TapGesture(tapPoint));

            return this.JsonResponse();
        }

        #endregion
    }
}
