namespace WindowsPhoneDriver.InnerDriver.Commands.FindByHelpers
{
    using System;
    using System.Windows;

    internal class By
    {
        #region Constructors and Destructors

        public By(string strategy, string value)
        {
            if (strategy.Equals("tag name"))
            {
                this.Predicate = x => x.GetType().ToString().Equals(value);
            }
            else if (strategy.Equals("name"))
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
