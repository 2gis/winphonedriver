namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System.Collections.Generic;

    using WindowsPhoneDriver.Common;

    internal class TextCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            var text = string.Empty;

            var element = this.Automator.WebElements.GetRegisteredElement(this.ElementId);
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

            return Responder.CreateJsonResponse(ResponseStatus.Success, text);
        }

        #endregion
    }
}
