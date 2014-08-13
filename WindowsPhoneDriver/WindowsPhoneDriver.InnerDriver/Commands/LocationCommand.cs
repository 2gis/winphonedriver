﻿namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System.Collections.Generic;

    using WindowsPhoneDriver.Common;

    internal class LocationCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            var element = this.Automator.WebElements.GetRegisteredElement(this.ElementId);
            var coordinates = element.GetCoordinates(this.Automator.VisualRoot);
            var coordinatesDict = new Dictionary<string, int>
                                      {
                                          { "x", (int)coordinates.X }, 
                                          { "y", (int)coordinates.Y }
                                      };

            return Responder.CreateJsonResponse(ResponseStatus.Success, coordinatesDict);
        }

        #endregion
    }
}
