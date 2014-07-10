namespace WindowsPhoneDriver.InnerDriver
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;

    using Newtonsoft.Json;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.InnerDriver.Commands;

    internal partial class Automator
    {
        #region Static Fields

        private static int safeInstanceCount;

        #endregion

        #region Fields

        private readonly UIElement visualRoot;

        private readonly Dictionary<string, FrameworkElement> webElements;

        #endregion

        #region Constructors and Destructors

        public Automator(UIElement visualRoot)
        {
            this.webElements = new Dictionary<string, FrameworkElement>();
            this.visualRoot = visualRoot;
        }

        #endregion

        #region Public Methods and Operators

        public string PerformAlertCommand(bool accept)
        {
            var command = new AlertCommand
                              {
                                  WebElements = this.webElements, 
                                  VisualRoot = this.visualRoot, 
                                  Action = accept ? AlertCommand.With.Accept : AlertCommand.With.Dismiss, 
                              };
            return command.Do();
        }

        public string PerformClickCommand(string elementId)
        {
            var command = new ClickCommand
                              {
                                  WebElements = this.webElements, 
                                  VisualRoot = this.visualRoot, 
                                  ElementId = elementId
                              };
            return command.Do();
        }

        public string PerformDisplayedCommand(string elementId)
        {
            var command = new DisplayedCommand
                              {
                                  WebElements = this.webElements, 
                                  VisualRoot = this.visualRoot, 
                                  ElementId = elementId
                              };
            return command.Do();
        }

        public string PerformLocationCommand(string elementId)
        {
            var command = new LocationCommand
                              {
                                  WebElements = this.webElements, 
                                  VisualRoot = this.visualRoot, 
                                  ElementId = elementId
                              };
            return command.Do();
        }

        public string PerformTextCommand(string elementId)
        {
            var command = new TextCommand
                              {
                                  WebElements = this.webElements, 
                                  VisualRoot = this.visualRoot, 
                                  ElementId = elementId
                              };
            return command.Do();
        }

        public string PerformValueCommand(string elementId, string content)
        {
            var command = new ValueCommand
                              {
                                  WebElements = this.webElements, 
                                  VisualRoot = this.visualRoot, 
                                  ElementId = elementId, 
                                  KeyString = RequestParser.GetKeysString(content)
                              };
            return command.Do();
        }

        public string PerfromAlertTextCommand()
        {
            var command = new AlertTextCommand { WebElements = this.webElements, VisualRoot = this.visualRoot, };
            return command.Do();
        }

        public string ProcessCommand(string urn, string content)
        {
            var response = string.Empty;
            var command = RequestParser.GetUrnLastToken(urn);
            string elementId;
            var urnLength = RequestParser.GetUrnTokensCount(urn);
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
                    var elementObject = JsonConvert.DeserializeObject<JsonFindElementObjectContent>(content);

                    switch (urnLength)
                    {
                        case 3:

                            // this is an absolute elements command ("/session/:sessionId/element"), search from root
                            response = this.PerformElementCommand(elementObject, null);
                            break;
                        case 5:

                            // this is a relative elements command("/session/:sessionId/element/:id/element"), search from specific element
                            var relativeElementId = RequestParser.GetElementId(urn);
                            response = this.PerformElementCommand(elementObject, relativeElementId);
                            break;
                    }

                    break;

                case "elements":
                    var elementsObject = JsonConvert.DeserializeObject<JsonFindElementObjectContent>(content);

                    switch (urnLength)
                    {
                        case 3:

                            // this is an absolute elements command ("/session/:sessionId/element"), search from root
                            response = this.PerformElementsCommand(elementsObject, null);
                            break;
                        case 5:

                            // this is a relative elements command("/session/:sessionId/element/:id/element"), search from specific element
                            var relativeElementId = RequestParser.GetElementId(urn);
                            response = this.PerformElementsCommand(elementsObject, relativeElementId);
                            break;
                    }

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

            commandToExecute.VisualRoot = this.visualRoot;
            commandToExecute.WebElements = this.webElements;

            response = commandToExecute.Do();
            return response;
        }

        #endregion

        #region Methods

        private string AddElementToWebElements(FrameworkElement element)
        {
            var webElementId = this.webElements.FirstOrDefault(x => x.Value == element).Key;

            if (webElementId == null)
            {
                Interlocked.Increment(ref safeInstanceCount);

                webElementId = element.GetHashCode() + "-" + safeInstanceCount.ToString(string.Empty);
                this.webElements.Add(webElementId, element);
            }

            return webElementId;
        }

        #endregion
    }
}
