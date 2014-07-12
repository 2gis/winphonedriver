namespace WindowsPhoneDriver.InnerDriver
{
    using System.Windows;

    using Newtonsoft.Json;

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

        public string ProcessCommand(string urn, string content)
        {
            var response = string.Empty;
            var command = RequestParser.GetUrnLastToken(urn);
            string elementId;
            CommandBase commandToExecute = null;
            switch (command)
            {
                case "ping":
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, "ping");
                    break;

                case "alert_text":
                    commandToExecute = new AlertTextCommand();
                    break;

                case "accept_alert":
                    commandToExecute = new AlertCommand { Action = AlertCommand.With.Accept };
                    break;

                case "dismiss_alert":
                    commandToExecute = new AlertCommand { Action = AlertCommand.With.Dismiss };
                    break;

                case "element":
                    elementId = RequestParser.GetElementId(urn);
                    commandToExecute = new ElementCommand
                                           {
                                               ElementId = elementId, 
                                               SearchParameters =
                                                   JsonConvert
                                                   .DeserializeObject<JsonFindElementObjectContent>(
                                                       content)
                                           };

                    break;

                case "elements":
                    elementId = RequestParser.GetElementId(urn);
                    commandToExecute = new ElementsCommand
                                           {
                                               ElementId = elementId, 
                                               SearchParameters =
                                                   JsonConvert
                                                   .DeserializeObject<JsonFindElementObjectContent>(
                                                       content)
                                           };

                    break;

                case "click":
                    elementId = RequestParser.GetElementId(urn);
                    commandToExecute = new ClickCommand { ElementId = elementId };
                    break;

                case "value":
                    elementId = RequestParser.GetElementId(urn);
                    commandToExecute = new ValueCommand
                                           {
                                               ElementId = elementId, 
                                               KeyString = RequestParser.GetKeysString(content)
                                           };
                    break;

                case "text":
                    elementId = RequestParser.GetElementId(urn);
                    commandToExecute = new TextCommand { ElementId = elementId };
                    break;

                case "displayed":
                    elementId = RequestParser.GetElementId(urn);
                    commandToExecute = new DisplayedCommand { ElementId = elementId };
                    break;

                case "location":
                    elementId = RequestParser.GetElementId(urn);
                    commandToExecute = new LocationCommand { ElementId = elementId };
                    break;

                default:
                    response = "Unimplemented";
                    break;
            }

            if (commandToExecute == null)
            {
                return response;
            }

            // TODO: Replace passing Automator to command with passing some kind of configuration
            commandToExecute.Automator = this;

            response = commandToExecute.Do();
            return response;
        }

        #endregion
    }
}
