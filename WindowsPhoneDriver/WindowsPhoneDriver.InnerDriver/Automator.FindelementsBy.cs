namespace WindowsPhoneDriver.InnerDriver
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.InnerDriver.Commands;

    internal partial class Automator
    {
        #region Public Methods and Operators

        public string PerformElementsCommand(JsonFindElementObjectContent elementObject, string relativeElementId)
        {
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

            // think of a prettier way to do this - without modifying names
            var result = new List<JsonWebElementContent>();
            if (searchPolicy.Equals("tag name"))
            {
                var foundObjectsIdList = this.FindElementsByType(elementId, relativeElement);
                result.AddRange(foundObjectsIdList.Select(foundObjectId => new JsonWebElementContent(foundObjectId)));
            }

            var response = result.Count != 0
                               ? Responder.CreateJsonResponse(ResponseStatus.Success, result.ToArray())
                               : Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);

            return response;
        }

        #endregion

        #region Methods

        private IEnumerable<string> FindElementsByType(string typeName, DependencyObject relativeElement)
        {
            var foundIds = new List<string>();

            UiHelpers.BeginInvokeSync(
                () =>
                    {
                        foreach (var element in
                            VisualTreeHelperMethods.GetDescendantsOfTypeByPredicate(relativeElement, typeName))
                        {
                            foundIds.Add(this.AddElementToWebElements((FrameworkElement)element));
                        }
                    });
            return foundIds;
        }

        #endregion
    }
}
