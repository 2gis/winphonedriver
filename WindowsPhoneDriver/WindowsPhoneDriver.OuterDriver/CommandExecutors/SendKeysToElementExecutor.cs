namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System.Linq;
    using System.Windows.Forms;

    internal class SendKeysToElementExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            // if the text has the ENTER command in it, execute it after sending the rest of the text to the inner driver
            var needToClickEnter = false;
            var originalContent = ExecutedCommand.Parameters;
            var value = ((object[])originalContent["value"]).Select(o => o.ToString()).ToArray();

            const string EnterKey = "\ue007";

            if (value.Contains(EnterKey))
            {
                needToClickEnter = true;
                value = value.Where(val => val != EnterKey).ToArray();
            }

            ExecutedCommand.Parameters["value"] = value;

            // TODO check if response status = success, throw if not
            var responseBody = this.Automator.CommandForwarder.ForwardCommand(ExecutedCommand);

            if (needToClickEnter)
            {
                this.Automator.EmulatorController.TypeKey(Keys.Enter);
            }

            return null;
        }

        #endregion
    }
}
