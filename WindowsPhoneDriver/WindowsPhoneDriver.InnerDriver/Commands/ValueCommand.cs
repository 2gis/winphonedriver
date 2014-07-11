namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Controls;

    using WindowsPhoneDriver.Common;

    internal class ValueCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        public string KeyString { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            var webElements = this.Automator.WebElements;
            string response;
            FrameworkElement valueElement;
            if (webElements.TryGetValue(this.ElementId, out valueElement))
            {
                var textbox = valueElement as TextBox;
                if (textbox != null)
                {
                    TrySetText(textbox, this.KeyString);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, null);
                }
                else
                {
                    throw new AutomationException("Element referenced is not a TextBox.", ResponseStatus.UnknownError);
                }
            }
            else
            {
                throw new AutomationException("Element referenced is no longer attached to the page's DOM.", ResponseStatus.StaleElementReference);
            }

            return response;
        }

        #endregion

        #region Methods

        private static void TrySetText(TextBox textbox, string text)
        {
            Deployment.Current.Dispatcher.BeginInvoke(
                () =>
                    {
                        var peer = new TextBoxAutomationPeer(textbox);
                        var valueProvider = peer.GetPattern(PatternInterface.Value) as IValueProvider;
                        if (valueProvider != null)
                        {
                            valueProvider.SetValue(text);
                        }

                        textbox.Focus();
                    });
        }

        #endregion
    }
}
