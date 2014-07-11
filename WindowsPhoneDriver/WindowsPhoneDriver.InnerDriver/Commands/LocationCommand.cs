namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System.Collections.Generic;
    using System.Windows;

    using WindowsPhoneDriver.Common;

    internal class LocationCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            string response;
            FrameworkElement valueElement;
            if (this.Automator.WebElements.TryGetValue(this.ElementId, out valueElement))
            {
                var coordinates = VisualTreeHelperMethods.GetCoordinates(valueElement, this.Automator.VisualRoot);
                var coordinatesDict = new Dictionary<string, int>
                                          {
                                              { "x", (int)coordinates.X }, 
                                              { "y", (int)coordinates.Y }
                                          };
                response = Responder.CreateJsonResponse(ResponseStatus.Success, coordinatesDict);
            }
            else
            {
                throw new AutomationException("Element referenced is no longer attached to the page's DOM.", ResponseStatus.StaleElementReference);
            }

            return response;
        }

        #endregion
    }
}
