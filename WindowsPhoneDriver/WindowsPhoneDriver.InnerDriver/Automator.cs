namespace WindowsPhoneDriver.InnerDriver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Controls;
    using System.Windows.Media;

    using WindowsPhoneDriver.Common;

    internal class Automator
    {
        #region Static Fields

        private static int safeInstanceCount;

        #endregion

        #region Fields

        private readonly List<Point> points; // ugly temporary (I hope) workaround to get objects from the UI thread

        private readonly UIElement visualRoot;

        private readonly Dictionary<string, FrameworkElement> webElements;

        #endregion

        #region Constructors and Destructors

        public Automator(UIElement visualRoot)
        {
            this.webElements = new Dictionary<string, FrameworkElement>();
            this.visualRoot = visualRoot;
            this.points = new List<Point>();
        }

        #endregion

        #region Public Methods and Operators

        public void ClosePopups(bool accert = true)
        {
            // Will work only with CustomMessageBox or other types of pop-us that have left and right button
            var buttonName = accert ? "LeftButton" : "RightButton";
            UiHelpers.BeginInvokeSync(
                () =>
                    {
                        var popups = VisualTreeHelper.GetOpenPopups();
                        foreach (var popup in popups)
                        {
                            // TODO: Press x:Name:LeftButton instead of simple closing 
                            var popupChild = popup.Child;
                            var element =
                                (FrameworkElement)
                                GetDescendantsOfNameByPredicate(popupChild, buttonName).FirstOrDefault();
                            if (!(element is Button))
                            {
                                continue;
                            }

                            var peer = new ButtonAutomationPeer(element as Button);
                            var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                            if (invokeProv != null)
                            {
                                invokeProv.Invoke();
                            }
                        }
                    });
        }

        public string FirstPopupText()
        {
            var message = string.Empty;

            UiHelpers.BeginInvokeSync(
                () =>
                    {
                        var popups = VisualTreeHelper.GetOpenPopups();
                        foreach (var popup in popups)
                        {
                            var popupChild = popup.Child;
                            var elements = GetDescendantsOfTypeByPredicate(
                                popupChild, 
                                "System.Windows.Controls.TextBlock");
                            foreach (var textBlock in elements.Select(dependencyObject => dependencyObject as TextBlock))
                            {
                                if (textBlock != null)
                                {
                                    message = textBlock.Text;
                                }

                                if (!string.IsNullOrEmpty(message))
                                {
                                    break;
                                }
                            }
                        }
                    });
            return string.IsNullOrEmpty(message)
                       ? Responder.CreateJsonResponse(ResponseStatus.NoAlertOpenError, null)
                       : Responder.CreateJsonResponse(ResponseStatus.Success, message);
        }

        public string PerformClickCommand(string elementId)
        {
            string response;
            FrameworkElement element;
            if (this.webElements.TryGetValue(elementId, out element))
            {
                // commented part is working, but is disable so that click will work the same way for all the elements

                // Button button = element as Button;
                // if (button != null) {
                // TryClick(button as Button);
                // response = Responder.CreateJsonResponse(ResponseStatus.Success, null);
                // }
                // else {
                Point coordinates;
                this.GetElementCoordinates(element);
                coordinates = this.points.First();
                this.points.RemoveAt(0);
                var strCoordinates = coordinates.X + ":" + coordinates.Y;
                response = Responder.CreateJsonResponse(ResponseStatus.UnknownError, strCoordinates);

                // }
            }
            else
            {
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }

            return response;
        }

        public string PerformDisplayedCommand(string elementId)
        {
            string response;
            FrameworkElement element;
            if (this.webElements.TryGetValue(elementId, out element))
            {
                var displayed = true;

                UiHelpers.BeginInvokeSync(() => { displayed = element.IsUserVisible(); });

                response = Responder.CreateJsonResponse(ResponseStatus.Success, displayed);
            }
            else
            {
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }

            return response;
        }

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

        public string PerformElementsCommand(JsonFindElementObjectContent elementObject, string relativeElementId)
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

            // think of a prettier way to do this - without modifying names
            var result = new List<JsonWebElementContent>();
            if (searchPolicy.Equals("tag name"))
            {
                var foundObjectsIdList = this.FindElementsByType(elementId, relativeElement);
                result.AddRange(foundObjectsIdList.Select(foundObjectId => new JsonWebElementContent(foundObjectId)));
            }

            if (result.Count != 0)
            {
                response = Responder.CreateJsonResponse(ResponseStatus.Success, result.ToArray());
            }
            else
            {
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }

            return response;
        }

        public string PerformLocationCommand(string elementId)
        {
            string response;
            FrameworkElement valueElement;
            if (this.webElements.TryGetValue(elementId, out valueElement))
            {
                Point coordinates;
                this.GetElementCoordinates(valueElement);
                coordinates = this.points.First();
                this.points.RemoveAt(0);
                var coordinatesDict = new Dictionary<string, int>
                                          {
                                              { "x", (int)coordinates.X }, 
                                              { "y", (int)coordinates.Y }
                                          };
                response = Responder.CreateJsonResponse(ResponseStatus.Success, coordinatesDict);
            }
            else
            {
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }

            return response;
        }

        public string PerformTextCommand(string elementId)
        {
            var text = string.Empty;
            string response;
            FrameworkElement element;
            if (this.webElements.TryGetValue(elementId, out element))
            {
                var properyNames = new List<string> { "Text", "Content" };

                foreach (var propertyName in properyNames)
                {
                    // list of Text property aliases. Use "Text" for TextBox, TextBlock, etc. Use "Content" as fallback if there is no "Text" property
                    var textProperty = element.GetType().GetProperty(propertyName);
                    if (textProperty != null)
                    {
                        UiHelpers.BeginInvokeSync(() => { text = textProperty.GetValue(element, null).ToString(); });
                        break;
                    }
                }

                response = Responder.CreateJsonResponse(ResponseStatus.Success, text);
            }
            else
            {
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }

            return response;
        }

        public string PerformValueCommand(string elementId, string content)
        {
            string response;
            FrameworkElement valueElement;
            if (this.webElements.TryGetValue(elementId, out valueElement))
            {
                var textbox = valueElement as TextBox;
                string jsonValue = RequestParser.GetKeysString(content);
                if (textbox != null)
                {
                    this.TrySetText(textbox, jsonValue);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, null);
                }
                else
                {
                    response = Responder.CreateJsonResponse(ResponseStatus.UnknownError, null);
                }
            }
            else
            {
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }

            return response;
        }

        #endregion

        #region Methods

        private static IEnumerable<DependencyObject> GetDescendantsByPredicate(
            DependencyObject rootItem, 
            Predicate<DependencyObject> predicate)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(rootItem);
            for (var i = 0; i < childrenCount; ++i)
            {
                var child = VisualTreeHelper.GetChild(rootItem, i);
                if (predicate(child))
                {
                    yield return child;
                }

                foreach (var grandChild in GetDescendantsByPredicate(child, predicate))
                {
                    yield return grandChild;
                }
            }
        }

        private static IEnumerable<DependencyObject> GetDescendantsOfNameByPredicate(DependencyObject item, string name)
        {
            return GetDescendantsByPredicate(item, x => ((FrameworkElement)x).Name.Equals(name));
        }

        private static IEnumerable<DependencyObject> GetDescendantsOfTypeByPredicate(
            DependencyObject item, 
            string typeName)
        {
            return GetDescendantsByPredicate(item, x => x.GetType().ToString().Equals(typeName));
        }

        private string AddElementToWebElements(FrameworkElement element)
        {
            var webElementId = this.webElements.FirstOrDefault(x => x.Value == element).Key;

            if (webElementId == null)
            {
                Interlocked.Increment(ref safeInstanceCount);

                webElementId = element.GetHashCode() + "-" + safeInstanceCount.ToString(string.Empty);
                this.webElements.Add(webElementId, element);
            }

            return webElementId;
        }

        private string FindElementByName(string elementName, DependencyObject relativeElement)
        {
            string foundId = null;
            UiHelpers.BeginInvokeSync(
                () =>
                    {
                        var element =
                            (FrameworkElement)
                            GetDescendantsOfNameByPredicate(relativeElement, elementName).FirstOrDefault();
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
                            GetDescendantsOfTypeByPredicate(relativeElement, typeName).FirstOrDefault();
                        if (element != null)
                        {
                            foundId = this.AddElementToWebElements(element);
                        }
                    });
            return foundId;
        }

        private IEnumerable<string> FindElementsByType(string typeName, DependencyObject relativeElement)
        {
            var foundIds = new List<string>();

            UiHelpers.BeginInvokeSync(
                () =>
                    {
                        foreach (var element in GetDescendantsOfTypeByPredicate(relativeElement, typeName))
                        {
                            foundIds.Add(this.AddElementToWebElements((FrameworkElement)element));
                        }
                    });
            return foundIds;
        }

        private void GetElementCoordinates(FrameworkElement element)
        {
            UiHelpers.BeginInvokeSync(
                () =>
                    {
                        var point = element.TransformToVisual(this.visualRoot).Transform(new Point(0, 0));
                        var center = new Point(
                            point.X + (int)(element.ActualWidth / 2), 
                            point.Y + (int)(element.ActualHeight / 2));
                        this.points.Add(center);
                    });
        }

        /*
        private void TryClick(Button button)
        {
            Deployment.Current.Dispatcher.BeginInvoke(
                () =>
                    {
                        var peer = new ButtonAutomationPeer(button);
                        var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                        if (invokeProv != null)
                        {
                            invokeProv.Invoke();
                        }
                    });
        }
*/
        private void TrySetText(TextBox textbox, string text)
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
