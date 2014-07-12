﻿namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using WindowsPhoneDriver.Common;

    internal class DisplayedCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            var element = this.Automator.WebElements.GetRegisteredElement(this.ElementId);
            var displayed = element.IsUserVisible();

            return Responder.CreateJsonResponse(ResponseStatus.Success, displayed);
        }

        #endregion
    }
}