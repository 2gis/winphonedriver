namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System.Windows;

    using WindowsPhoneDriver.Common;

    internal class ClickCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            // Warning: this method does not actually click, it gets coordinates for use in outerdriver.
            string response;
            FrameworkElement element;
            if (this.Automator.WebElements.TryGetValue(this.ElementId, out element))
            {
                // TODO: Replace with implementation using AutomationPeer
                var coordinates = VisualTreeHelperMethods.GetCoordinates(element, this.Automator.VisualRoot);
                var strCoordinates = coordinates.X + ":" + coordinates.Y;
                response = Responder.CreateJsonResponse(ResponseStatus.UnknownError, strCoordinates);
            }
            else
            {
                // TODO: Create convenience methods for initializing AutomationExceptionwith prebuilt messages?
                throw new AutomationException("Element referenced is no longer attached to the page's DOM.", ResponseStatus.StaleElementReference);
            }

            return response;
        }

        #endregion
    }
}
