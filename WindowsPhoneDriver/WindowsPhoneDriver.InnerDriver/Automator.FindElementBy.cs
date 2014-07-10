namespace WindowsPhoneDriver.InnerDriver
{
    using System.Linq;
    using System.Windows;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.InnerDriver.Commands;

    internal partial class Automator
    {
        #region Public Methods and Operators

        public string PerformElementCommand(JsonFindElementObjectContent elementObject, string relativeElementId)
        {
            string response;
            var elementId = elementObject.Value;
            var searchPolicy = elementObject.UsingMethod;
            DependencyObject relativeElement;

            if (relativeElementId == null)
            {
                relativeElement = this.visualRoot;
            }
            else
            {
                FrameworkElement possibleRelativeElement;
                this.webElements.TryGetValue(relativeElementId, out possibleRelativeElement);
                relativeElement = possibleRelativeElement ?? this.visualRoot;
            }

            if (this.webElements.ContainsKey(elementId))
            {
                var webElement = new JsonWebElementContent(elementId);
                response = Responder.CreateJsonResponse(0, webElement);
            }
            else
            {
                string webObjectId = null;

                if (searchPolicy.Equals("name"))
                {
                    webObjectId = this.FindElementByName(elementId, relativeElement);
                }
                else if (searchPolicy.Equals("tag name"))
                {
                    webObjectId = this.FindElementByType(elementId, relativeElement);
                }

                if (webObjectId != null)
                {
                    var webElement = new JsonWebElementContent(webObjectId);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, webElement);
                }
                else
                {
                    response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
                }
            }

            return response;
        }

        #endregion

        #region Methods

        private string FindElementByName(string elementName, DependencyObject relativeElement)
        {
            string foundId = null;
            UiHelpers.BeginInvokeSync(
                () =>
                    {
                        var element =
                            (FrameworkElement)
                            VisualTreeHelperMethods.GetDescendantsOfNameByPredicate(relativeElement, elementName)
                                .FirstOrDefault();
                        if (element != null)
                        {
                            foundId = this.AddElementToWebElements(element);
                        }
                    });

            return foundId;
        }

        // TODO: Refactor. Use same signature for FindElementBy* and FindElementsBy
        // TODO: Replace PerformElementCommand and PerformElementsCommand with single method
        private string FindElementByType(string typeName, DependencyObject relativeElement)
        {
            string foundId = null;
            UiHelpers.BeginInvokeSync(
                () =>
                    {
                        var element =
                            (FrameworkElement)
                            VisualTreeHelperMethods.GetDescendantsOfTypeByPredicate(relativeElement, typeName)
                                .FirstOrDefault();
                        if (element != null)
                        {
                            foundId = this.AddElementToWebElements(element);
                        }
                    });
            return foundId;
        }

        #endregion
    }
}
