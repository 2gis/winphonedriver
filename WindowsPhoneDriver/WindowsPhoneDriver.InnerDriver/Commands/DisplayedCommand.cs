namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System.Windows;

    using WindowsPhoneDriver.Common;

    internal class DisplayedCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            string response;
            FrameworkElement element;
            if (this.Automator.WebElements.TryGetValue(this.ElementId, out element))
            {
                var displayed = element.IsUserVisible();

                response = Responder.CreateJsonResponse(ResponseStatus.Success, displayed);
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
