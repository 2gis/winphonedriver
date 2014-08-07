namespace WindowsPhoneDriver.InnerDriver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.InnerDriver.Commands;

    internal class Automator
    {
        #region Constructors and Destructors

        public Automator(UIElement visualRoot)
        {
            this.VisualRoot = visualRoot;
            this.WebElements = new AutomatorElements();
        }

        #endregion

        #region Public Properties

        public UIElement VisualRoot { get; private set; }

        public AutomatorElements WebElements { get; private set; }

        #endregion

        #region Public Methods and Operators

        public string ProcessCommand(string content)
        {
            var requestData = JsonConvert.DeserializeObject<JsonCommand>(content);
            var command = requestData.Name;
            var parameters = requestData.Parameters;

            string elementId = null;
            if (parameters == null)
            {
                throw new NullReferenceException("Parameteres can not be NULL");
            }

            object elementIdObject;
            if (parameters.TryGetValue("ID", out elementIdObject))
            {
                elementId = elementIdObject.ToString();
            }

            CommandBase commandToExecute;

            if (command.Equals(DriverCommand.GetAlertText))
            {
                commandToExecute = new AlertTextCommand();
            }
            else if (command.Equals(DriverCommand.AcceptAlert))
            {
                commandToExecute = new AlertCommand { Action = AlertCommand.With.Accept };
            }
            else if (command.Equals(DriverCommand.DismissAlert))
            {
                commandToExecute = new AlertCommand { Action = AlertCommand.With.Dismiss };
            }
            else if (command.Equals(DriverCommand.FindElement) || command.Equals(DriverCommand.FindChildElement))
            {
                commandToExecute = new ElementCommand { ElementId = elementId };
            }
            else if (command.Equals(DriverCommand.FindElements) || command.Equals(DriverCommand.FindChildElements))
            {
                commandToExecute = new ElementsCommand { ElementId = elementId };
            }
            else if (command.Equals(DriverCommand.ClickElement))
            {
                commandToExecute = new ClickCommand { ElementId = elementId };
            }
            else if (command.Equals(DriverCommand.SendKeysToElement))
            {
                var values = ((JArray)parameters["value"]).ToObject<List<string>>();
                var value = values.Aggregate((aggregated, next) => aggregated + next);
                commandToExecute = new ValueCommand { ElementId = elementId, KeyString = value };
            }
            else if (command.Equals(DriverCommand.GetElementText))
            {
                commandToExecute = new TextCommand { ElementId = elementId };
            }
            else if (command.Equals(DriverCommand.IsElementDisplayed))
            {
                commandToExecute = new DisplayedCommand { ElementId = elementId };
            }
            else if (command.Equals(DriverCommand.GetElementLocation))
            {
                commandToExecute = new LocationCommand { ElementId = elementId };
            }
            else
            {
                return "<UnimplementedCommand>";
            }

            // TODO: Replace passing Automator to command with passing some kind of configuration
            commandToExecute.Automator = this;
            commandToExecute.Parameters = parameters;

            var response = commandToExecute.Do();
            return response;
        }

        #endregion
    }
}
