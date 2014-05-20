using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;

namespace WindowsPhoneJsonWireServer {
    class Automator {

        private Dictionary<String, FrameworkElement> webElements;
        private UIElement visualRoot;
        private List<Point> points; //ugly temporary (i hope) workaround to get objects from the UI thread
        private int elementsCount; //same here

        public Automator(UIElement visualRoot) {
            this.webElements = new Dictionary<string, FrameworkElement>();
            this.visualRoot = visualRoot;
            this.points = new List<Point>();
        }

        public String PerformTextCommand(String elementId) {
            String text = String.Empty;
            String response = String.Empty;
            FrameworkElement element;
            if (webElements.TryGetValue(elementId, out element)) {
                if (element is TextBlock)
                    text = (element as TextBlock).Text;
                else if (element is TextBox)
                    text = (element as TextBox).Text;

                response = Responder.CreateJsonResponse(ResponseStatus.Success, text);
            }
            else
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            return response;
        }

        public String PerformElementCommand(FindElementObject elementObject) {
            String response = String.Empty;
            String elementId = elementObject.getValue();
            String searchPolicy = elementObject.usingMethod;
            if (webElements.ContainsKey(elementId)) {
                var webElement = new WebElement(elementId);
                response = Responder.CreateJsonResponse(0, webElement);
            }
            else {
                if (searchPolicy.Equals("name"))
                    FindElementByName(elementId);
                else if (searchPolicy.Equals("tag name"))
                    FindElementByType(elementId);
                //if the element has been sucessfully added
                if (webElements.ContainsKey(elementId)) {
                    var webElement = new WebElement(elementId);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, webElement);
                }
                else
                    response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }
            return response;
        }

        public String PerformElementsCommand(FindElementObject elementObject) {
            elementsCount = 0;
            String response = String.Empty;
            String elementId = elementObject.getValue();
            String searchPolicy = elementObject.usingMethod;
            //think of a prettier way to do this - without modifying names
            List<WebElement> result = new List<WebElement>();
            if (searchPolicy.Equals("tag name"))
                FindElementsByType(elementId);
            
            //if something has been added to the collection, get it out and return it;
            for (int current = 0; current < elementsCount; current++) {
                String currentId = elementId + current.ToString();
                if (webElements.ContainsKey(currentId)) 
                result.Add(new WebElement(currentId));
            }
            if (result.Count != 0)
                response = Responder.CreateJsonResponse(ResponseStatus.Success, result.ToArray());
            //if the elementы have been sucessfully added
            
            else
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);

            return response;

        }

        public String PerformClickCommand(String elementId) {
            String response = String.Empty;
            FrameworkElement element;
            if (webElements.TryGetValue(elementId, out element)) {
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

        public String PerformValueCommand(String elementId, String content) {
            String response = String.Empty;
            FrameworkElement valueElement;
            if (webElements.TryGetValue(elementId, out valueElement)) {
                TextBox textbox = valueElement as TextBox;
                String jsonValue = Parser.GetKeysString(content);
                if (textbox != null) {
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

        public String PerformLocationCommand(String elementId) {
            String response = String.Empty;
            FrameworkElement valueElement;
            if (webElements.TryGetValue(elementId, out valueElement)) {
                var coordinates = new Point();
                GetElementCoordinates(valueElement);
                coordinates = points.First();
                points.RemoveAt(0);
                var coordinatesDict = new Dictionary<String, Int32>();
                coordinatesDict.Add("x", (int)coordinates.X);
                coordinatesDict.Add("y", (int)coordinates.Y);
                response = Responder.CreateJsonResponse(ResponseStatus.Success, coordinatesDict);
            }
            else
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            return response;
        }

        private void GetElementCoordinates(FrameworkElement element) {
            EventWaitHandle wait = new AutoResetEvent(false);
            Deployment.Current.Dispatcher.BeginInvoke(() => {

                var point = element.TransformToVisual(visualRoot).Transform(new Point(0, 0));
                Point center = new Point(point.X + (int)element.ActualWidth / 2, point.Y + (int)element.ActualHeight / 2);
                points.Add(center);
                wait.Set();
            });
            wait.WaitOne();
        }

        private void TryClick(Button button) {
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(button);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            });
        }

        private void TrySetText(TextBox textbox, String text) {
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                TextBoxAutomationPeer peer = new TextBoxAutomationPeer(textbox);
                IValueProvider valueProvider = peer.GetPattern(PatternInterface.Value) as IValueProvider;
                valueProvider.SetValue(text);
                textbox.Focus();
            });
        }

        //ugly search workaround
        private void FindElementByName(String elementName) {
            FrameworkElement element = null;
            //Used to wait until the element is actually added
            EventWaitHandle wait = new AutoResetEvent(false);
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                var grids = GetDescendants<Grid>(visualRoot);
                var topGrid = grids.First() as FrameworkElement;
                if (topGrid != null) {
                    //element = topGrid.FindName(elementName) as FrameworkElement;
                    element = GetDescendantsOfName(topGrid, elementName) as FrameworkElement;
                    if (element != null)
                        webElements.Add(elementName, element);
                    wait.Set();
                }
            });
            wait.WaitOne();
        }

        private void FindElementByType(String typeName) {
            FrameworkElement element = null;
            //Used to wait until the element is actually added
            EventWaitHandle wait = new AutoResetEvent(false);
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                var elements = GetDescendantsOfTypeName(visualRoot, typeName);
                if (elements.Count() != 0) {
                    element = elements.First() as FrameworkElement;
                    if (element != null)
                        webElements.Add(typeName, element);
                }
                wait.Set();
            });
            wait.WaitOne();
        }

        private void FindElementsByType(String typeName) {
            FrameworkElement element = null;
            //Used to wait until the element is actually added
            EventWaitHandle wait = new AutoResetEvent(false);
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                var elements = GetDescendantsOfTypeName(visualRoot, typeName);
                if (elements.Count() != 0) {
                    int count = 0;
                    foreach (var nextElement in elements) {
                        element = nextElement as FrameworkElement;
                        String webElementName = typeName + count.ToString();
                        if (element != null) {
                            //if the webElement is already cached, don't add it again, but still count the found ones
                            if (!webElements.ContainsKey(webElementName))
                                webElements.Add(webElementName, element);
                            count++;
                        }
                    }
                    elementsCount = count;
                }
                wait.Set();
            });
            wait.WaitOne();
        }

        private IEnumerable<DependencyObject> GetDescendants(DependencyObject item) {
            int childrenCount = VisualTreeHelper.GetChildrenCount(item);
            List<DependencyObject> children = new List<DependencyObject>();
            for (int i = 0; i < childrenCount; i++) {
                children.Add(VisualTreeHelper.GetChild(item, i));
            }

            foreach (var child in children) {
                yield return child;

                foreach (var grandChild in GetDescendants(child)) {
                    yield return grandChild;
                }
            }
        }

        private IEnumerable<DependencyObject> GetDescendants<T>(DependencyObject item) {
            int childrenCount = VisualTreeHelper.GetChildrenCount(item);
            List<DependencyObject> children = new List<DependencyObject>();
            for (int i = 0; i < childrenCount; i++) {
                children.Add(VisualTreeHelper.GetChild(item, i));
            }

            foreach (var child in children) {
                if (child is T)
                    yield return child;

                foreach (var grandChild in GetDescendants(child)) {
                    if (grandChild is T)
                        yield return grandChild;
                }
            }
        }

        private IEnumerable<DependencyObject> GetDescendantsOfTypeName(DependencyObject item, String typeName) {
            int childrenCount = VisualTreeHelper.GetChildrenCount(item);
            List<DependencyObject> children = new List<DependencyObject>();
            for (int i = 0; i < childrenCount; i++) {
                children.Add(VisualTreeHelper.GetChild(item, i));
            }

            foreach (var child in children) {
                if (child.GetType().ToString().Equals(typeName))
                    yield return child;

                foreach (var grandChild in GetDescendants(child)) {
                    if (grandChild.GetType().ToString().Equals(typeName))
                        yield return grandChild;
                }
            }
        }

        private DependencyObject GetDescendantsOfName(DependencyObject item, String name) {
            int childrenCount = VisualTreeHelper.GetChildrenCount(item);
            List<DependencyObject> children = new List<DependencyObject>();
            for (int i = 0; i < childrenCount; i++) {
                children.Add(VisualTreeHelper.GetChild(item, i));
            }

            foreach (var child in children) {
                if (((FrameworkElement)child).Name.Equals(name))
                    return child;

                foreach (var grandChild in GetDescendants(child)) {
                    if (((FrameworkElement)grandChild).Name.Equals(name))
                        return grandChild;
                }
            }
            return null;
        }


    }
}
