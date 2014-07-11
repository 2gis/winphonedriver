namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System;
    using System.Linq;
    using System.Windows;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.InnerDriver.Commands.FindByHelpers;

    internal class ElementCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        public JsonFindElementObjectContent SearchParameters { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            if (this.SearchParameters == null)
            {
                throw new InvalidOperationException("SearchParameters must be set before Do() is called");
            }

            string response;
            var searchValue = this.SearchParameters.Value;
            var searchPolicy = this.SearchParameters.UsingMethod;
            DependencyObject relativeElement;

            if (this.ElementId == null)
            {
                relativeElement = this.Automator.VisualRoot;
            }
            else
            {
                FrameworkElement possibleRelativeElement;
                this.Automator.WebElements.TryGetValue(this.ElementId, out possibleRelativeElement);
                relativeElement = possibleRelativeElement ?? this.Automator.VisualRoot;
            }

            if (this.Automator.WebElements.ContainsKey(searchValue))
            {
                var webElement = new JsonWebElementContent(searchValue);
                response = Responder.CreateJsonResponse(ResponseStatus.Success, webElement);
            }
            else
            {
                var searchStrategy = new By(searchPolicy, searchValue);
                var webObjectId = this.FindElementBy(relativeElement, searchStrategy);

                if (webObjectId != null)
                {
                    var webElement = new JsonWebElementContent(webObjectId);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, webElement);
                }
                else
                {
                    throw new AutomationException("Element cannot be found", ResponseStatus.NoSuchElement);
                }
            }

            return response;
        }

        #endregion

        #region Methods

        private string FindElementBy(DependencyObject relativeElement, By searchStrategy)
        {
            string foundId = null;

            var element = (FrameworkElement)Finder.GetDescendantsBy(relativeElement, searchStrategy).FirstOrDefault();
            if (element != null)
            {
                foundId = this.Automator.AddElementToWebElements(element);
            }

            return foundId;
        }

        #endregion
    }
}
