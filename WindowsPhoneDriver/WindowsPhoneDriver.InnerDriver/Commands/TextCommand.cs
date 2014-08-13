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
            var element = this.Automator.WebElements.GetRegisteredElement(this.ElementId);
            var text = element.GetText();
            
            return Responder.CreateJsonResponse(ResponseStatus.Success, text);
        }

        #endregion
    }
}
