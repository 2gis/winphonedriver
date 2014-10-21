namespace WindowsPhoneDriver.InnerDriver.Commands.FindByHelpers
{
    using System;
    using System.Windows;
    using System.Windows.Automation;

    internal class By
    {
        #region Constructors and Destructors

        public By(string strategy, string value)
        {
            if (strategy.Equals("tag name") || strategy.Equals("class name"))
            {
                this.Predicate = x => x.GetType().ToString().Equals(value);
            }
            else if (strategy.Equals("id"))
            {
                this.Predicate = x =>
                {
                    var automationId = x.GetValue(AutomationProperties.AutomationIdProperty) as string;
                    return automationId != null && automationId.Equals(value);
                };
            }
            else if (strategy.Equals("name"))
            {
                this.Predicate = x =>
                    {
                        var automationName = x.GetValue(AutomationProperties.NameProperty) as string;
                        return automationName != null && automationName.Equals(value);
                    };
            }
            else if (strategy.Equals("xname"))
            {
                this.Predicate = x =>
                    {
                        var frameworkElement = x as FrameworkElement;
                        return frameworkElement != null && frameworkElement.Name.Equals(value);
                    };
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Public Properties

        public Predicate<DependencyObject> Predicate { get; private set; }

        #endregion
    }
}
