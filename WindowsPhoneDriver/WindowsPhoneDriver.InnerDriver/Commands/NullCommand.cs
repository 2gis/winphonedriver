namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using WindowsPhoneDriver.Common;

    internal class NullCommand : CommandBase
    {
        #region Public Methods and Operators

        public override string DoImpl()
        {
            return Responder.CreateJsonResponse(ResponseStatus.Success, null);
        }

        #endregion
    }
}
