namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System.Collections.Generic;
    using System.Windows;

    using WindowsPhoneDriver.Common;

    internal class TextCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            var webElements = this.Automator.WebElements;
            var text = string.Empty;
            string response;
            FrameworkElement element;

            if (webElements.TryGetValue(this.ElementId, out element))
            {
                var propertyNames = new List<string> { "Text", "Content" };

                foreach (var propertyName in propertyNames)
                {
                    // list of Text property aliases. Use "Text" for TextBox, TextBlock, etc. Use "Content" as fallback if there is no "Text" property
                    var textProperty = element.GetType().GetProperty(propertyName);
                    if (textProperty != null)
                    {
                        text = textProperty.GetValue(element, null).ToString();
                        break;
                    }
                }

                response = Responder.CreateJsonResponse(ResponseStatus.Success, text);
            }
            else
            {
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }

            return response;
        }

        #endregion
    }
}
