namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Forms;
    using OpenQA.Selenium.Remote;

    using Common;
    using Common.Exceptions;

    internal class ExecuteScriptExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            const string MobileScriptPrefix = "mobile:";
            var script = this.ExecutedCommand.Parameters["script"].ToString();
            if (!script.StartsWith(MobileScriptPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException(
                    "execute partially implemented, supports only mobile: prefixed commands");
            }

            var command = script.Split(':')[1].ToLower(CultureInfo.InvariantCulture).Trim();

            if (command.Equals("start"))
            {
                this.Automator.EmulatorController.TypeKey(Keys.F2);
            }
            else if (command.Equals("search"))
            {
                this.Automator.EmulatorController.TypeKey(Keys.F3);
            }
            else if (command.Equals("invokeAppBarItem", StringComparison.OrdinalIgnoreCase))
            {
                var arguments = this.ExecutedCommand.Parameters["args"] as Array;
                if (arguments == null)
                {
                    throw new AutomationException("Bad parameters", ResponseStatus.JavaScriptError);
                }

                var itemType = arguments.GetValue(0);
                var index = arguments.GetValue(1);

                var parameters = new Dictionary<string, object>();
                parameters["itemType"] = itemType;
                parameters["index"] = index;

                var invokeCommand = new Command(this.ExecutedCommand.SessionId, ExtendedDriverCommand.InvokeAppBarItemCommand, parameters);
                return this.Automator.CommandForwarder.ForwardCommand(invokeCommand);
            }
            else
            {
                throw new AutomationException(
                    "Unknown 'mobile:' script command. See https://github.com/2gis/winphonedriver/wiki/Command-Execute-Script for supported commands.", 
                    ResponseStatus.JavaScriptError);
            }

            return this.JsonResponse();
        }

        #endregion
    }
}
