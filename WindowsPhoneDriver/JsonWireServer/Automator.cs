using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;

namespace WindowsPhoneJsonWireServer
{
    internal class Automator
    {
        private Dictionary<string, FrameworkElement> webElements;
        private UIElement visualRoot;
        private List<Point> points; //ugly temporary (i hope) workaround to get objects from the UI thread

        public Automator(UIElement visualRoot)
        {
            this.webElements = new Dictionary<string, FrameworkElement>();
            this.visualRoot = visualRoot;
            this.points = new List<Point>();
        }

        public void ClosePopups(bool accert = true)
        {
            // Will work only with CustomMessageBox or other types of pop-us that have left and right button
            var buttonName = accert ? "LeftButton" : "RightButton";
            UiHelpers.BeginInvokeSync(() =>
            {
                var popups = VisualTreeHelper.GetOpenPopups();
                foreach (var popup in popups)
                {
                    // TODO: Press x:Name:LeftButton instead of simple closing 
                    var popupChild = popup.Child;
                    var element =
                        (FrameworkElement) GetDescendantsOfNameByPredicate(popupChild, buttonName).FirstOrDefault();
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

        public String FirstPopupText()
        {
            var message = string.Empty;

            UiHelpers.BeginInvokeSync(() =>
            {
                var popups = VisualTreeHelper.GetOpenPopups();
                foreach (var popup in popups)
                {
                    var popupChild = popup.Child;
                    var elements = GetDescendantsOfTypeByPredicate(popupChild, "System.Windows.Controls.TextBlock");
                    foreach (var textBlock in elements.Select(dependencyObject => dependencyObject as TextBlock))
                    {
                        if (textBlock != null) message = textBlock.Text;
                        if (!String.IsNullOrEmpty(message))
                        {
                            break;
                        }
                    }
                }
            });
            return String.IsNullOrEmpty(message)
                ? Responder.CreateJsonResponse(ResponseStatus.NoAlertOpenError, null)
                : Responder.CreateJsonResponse(ResponseStatus.Success, message);
        }

        public String PerformDisplayedCommand(String elementId)
        {
            String text = String.Empty;
            String response;
            FrameworkElement element;
            if (webElements.TryGetValue(elementId, out element))
            {
                bool displayed = true;

                UiHelpers.BeginInvokeSync(() => { displayed = element.IsUserVisible(); });

                response = Responder.CreateJsonResponse(ResponseStatus.Success, displayed);
            }
            else
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            return response;
        }

        public String PerformTextCommand(String elementId)
        {
            String text = String.Empty;
            String response = String.Empty;
            FrameworkElement element;
            if (webElements.TryGetValue(elementId, out element))
            {
                var properyNames = new List<string>() {"Text", "Content"};
                System.Reflection.PropertyInfo textProperty = null;

                foreach (var propertyName in properyNames)
                {
                    // list of Text property aliases. Use "Text" for TextBox, TextBlock, etc. Use "Content" as fallback if there is no "Text" property
                    textProperty = element.GetType().GetProperty(propertyName);
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

        public String PerformElementCommand(FindElementObject elementObject, String relativeElementId)
        {
            String response = String.Empty;
            String elementId = elementObject.getValue();
            String searchPolicy = elementObject.usingMethod;
            DependencyObject relativeElement;

            if (relativeElementId == null)
            {
                relativeElement = visualRoot;
            }
            else
            {
                FrameworkElement possibleRelativeElement;
                webElements.TryGetValue(relativeElementId, out possibleRelativeElement);
                relativeElement = possibleRelativeElement ?? visualRoot;
            }

            if (webElements.ContainsKey(elementId))
            {
                var webElement = new WebElement(elementId);
                response = Responder.CreateJsonResponse(0, webElement);
            }
            else
            {
                string webObjectId = null;

                if (searchPolicy.Equals("name"))
                    webObjectId = FindElementByName(elementId, relativeElement);
                else if (searchPolicy.Equals("tag name"))
                    webObjectId = FindElementByType(elementId, relativeElement);
                if (webObjectId != null)
                {
                    var webElement = new WebElement(webObjectId);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, webElement);
                }
                else
                    response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }
            return response;
        }

        public String PerformElementsCommand(FindElementObject elementObject, String relativeElementId)
        {
            String response = String.Empty;
            String elementId = elementObject.getValue();
            String searchPolicy = elementObject.usingMethod;
            DependencyObject relativeElement;

            if (relativeElementId == null)
            {
                relativeElement = visualRoot;
            }
            else
            {
                FrameworkElement possibleRelativeElement;
                webElements.TryGetValue(relativeElementId, out possibleRelativeElement);
                relativeElement = possibleRelativeElement ?? visualRoot;
            }
            //think of a prettier way to do this - without modifying names
            var result = new List<WebElement>();
            if (searchPolicy.Equals("tag name"))
            {
                var foundObjectsIdList = FindElementsByType(elementId, relativeElement);
                result.AddRange(foundObjectsIdList.Select(foundObjectId => new WebElement(foundObjectId)));
            }
            if (result.Count != 0)
                response = Responder.CreateJsonResponse(ResponseStatus.Success, result.ToArray());
            else
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            return response;
        }

        public String PerformClickCommand(String elementId)
        {
            String response = String.Empty;
            FrameworkElement element;
            if (webElements.TryGetValue(elementId, out element))
            {
                //commented part is working, but is disable so that click will work the same way for all the elements

                //Button button = element as Button;
                //if (button != null) {
                //    TryClick(button as Button);
                //    response = Responder.CreateJsonResponse(ResponseStatus.Success, null);
                //}
                //else {

                var coordinates = new Point();
                GetElementCoordinates(element);
                coordinates = points.First();
                points.RemoveAt(0);
                String strCoordinates = coordinates.X + ":" + coordinates.Y;
                response = Responder.CreateJsonResponse(ResponseStatus.UnknownError, strCoordinates);
                //}
            }
            else
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            return response;
        }

        public String PerformValueCommand(String elementId, String content)
        {
            String response = String.Empty;
            FrameworkElement valueElement;
            if (webElements.TryGetValue(elementId, out valueElement))
            {
                TextBox textbox = valueElement as TextBox;
                String jsonValue = Parser.GetKeysString(content);
                if (textbox != null)
                {
                    TrySetText(textbox, jsonValue);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, null);
                }
                else
                    response = Responder.CreateJsonResponse(ResponseStatus.UnknownError, null);
            }
            else
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            return response;
        }

        public String PerformLocationCommand(String elementId)
        {
            String response = String.Empty;
            FrameworkElement valueElement;
            if (webElements.TryGetValue(elementId, out valueElement))
            {
                var coordinates = new Point();
                GetElementCoordinates(valueElement);
                coordinates = points.First();
                points.RemoveAt(0);
                var coordinatesDict = new Dictionary<String, Int32>();
                coordinatesDict.Add("x", (int) coordinates.X);
                coordinatesDict.Add("y", (int) coordinates.Y);
                response = Responder.CreateJsonResponse(ResponseStatus.Success, coordinatesDict);
            }
            else
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            return response;
        }

        private void GetElementCoordinates(FrameworkElement element)
        {
            EventWaitHandle wait = new AutoResetEvent(false);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var point = element.TransformToVisual(visualRoot).Transform(new Point(0, 0));
                Point center = new Point(point.X + (int) element.ActualWidth/2, point.Y + (int) element.ActualHeight/2);
                points.Add(center);
                wait.Set();
            });
            wait.WaitOne();
        }

        private void TryClick(Button button)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(button);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            });
        }

        private void TrySetText(TextBox textbox, String text)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                TextBoxAutomationPeer peer = new TextBoxAutomationPeer(textbox);
                IValueProvider valueProvider = peer.GetPattern(PatternInterface.Value) as IValueProvider;
                valueProvider.SetValue(text);
                textbox.Focus();
            });
        }

        private static int _safeInstanceCount = 0;

        private string AddElementToWebElements(FrameworkElement element)
        {
            var webElementId = webElements.FirstOrDefault(x => x.Value == element).Key;

            if (webElementId == null)
            {
                Interlocked.Increment(ref _safeInstanceCount);

                webElementId = element.GetHashCode().ToString("") + "-" + _safeInstanceCount.ToString("");
                webElements.Add(webElementId, element);
            }
            return webElementId;
        }

        private string FindElementByName(string elementName, DependencyObject relativeElement)
        {
            string foundId = null;
            UiHelpers.BeginInvokeSync(() =>
            {
                var element =
                    (FrameworkElement) GetDescendantsOfNameByPredicate(relativeElement, elementName).FirstOrDefault();
                if (element != null)
                {
                    foundId = AddElementToWebElements(element);
                }
            });

            return foundId;
        }

//      TODO: Refactor. Use same signature for FindElementBy* and FindElementsBy
//      TODO: Replace PerformElementCommand and PerformElementsCommand with single method

        private string FindElementByType(string typeName, DependencyObject relativeElement)
        {
            string foundId = null;
            UiHelpers.BeginInvokeSync(() =>
            {
                var element =
                    (FrameworkElement) GetDescendantsOfTypeByPredicate(relativeElement, typeName).FirstOrDefault();
                if (element != null)
                {
                    foundId = AddElementToWebElements(element);
                }
            });
            return foundId;
        }

        private List<string> FindElementsByType(string typeName, DependencyObject relativeElement)
        {
            var foundIds = new List<string>();

            UiHelpers.BeginInvokeSync(() =>
            {
                foreach (var element in GetDescendantsOfTypeByPredicate(relativeElement, typeName))
                {
                    foundIds.Add(AddElementToWebElements((FrameworkElement) element));
                }
            });
            return foundIds;
        }

        private static IEnumerable<DependencyObject> GetDescendantsByPredicate(DependencyObject rootItem,
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

        private static IEnumerable<DependencyObject> GetDescendantsOfNameByPredicate(DependencyObject item, String name)
        {
            return GetDescendantsByPredicate(item, x => ((FrameworkElement) x).Name.Equals(name));
        }

        private static IEnumerable<DependencyObject> GetDescendantsOfTypeByPredicate(DependencyObject item,
            String typeName)
        {
            return GetDescendantsByPredicate(item, x => ((FrameworkElement) x).GetType().ToString().Equals(typeName));
        }
    }
}