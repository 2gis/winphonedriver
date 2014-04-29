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

namespace WindowsPhoneJsonWireServer
{
    class Automator
    {

        private Dictionary<String, FrameworkElement> webElements;
        private UIElement visualRoot;
        private List<Point> points; //ugly temporary (i hope) workaround to get objects from the UI thread

        public Automator(UIElement visualRoot)
        {
            this.webElements = new Dictionary<string, FrameworkElement>();
            this.visualRoot = visualRoot;
            this.points = new List<Point>();
        }

        public String PerformTextCommand(String elementId)
        {
            String text = String.Empty;
            String response = String.Empty;
            FrameworkElement element;
            if (webElements.TryGetValue(elementId, out element))
            {
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

        public String PerformElementCommand(FindElementObject elementObject)
        {
            String response = String.Empty;
            //search for the element by it's name
            String elementId = elementObject.getValue();
            String searchPolicy = elementObject.usingMethod;
            if (webElements.ContainsKey(elementId))
            {
                var webElement = new WebElement(elementId);
                response = Responder.CreateJsonResponse(0, webElement);
            }
            else
            {
                if (searchPolicy.Equals("name"))
                    FindElementByName(elementId);
                else if (searchPolicy.Equals("tag name"))
                    FindElementByType(elementId);
                //if the element has been sucessfully added
                if (webElements.ContainsKey(elementId))
                {
                    var webElement = new WebElement(elementId);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, webElement);
                }
                else
                    response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }
            return response;
        }

        public String PerformClickCommand(String elementId)
        {
            String response = String.Empty;
            FrameworkElement element;
            if (webElements.TryGetValue(elementId, out element))
            {
                Button button = element as Button;
                if (button != null)
                {
                    TryClick(button as Button);
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, null);
                }
                else
                {
                    var coordinates = new Point();
                    GetElementCoordinates(element);
                    coordinates = points.First();
                    //Point leftUpper = element.TransformToVisual(visualRoot).Transform(new Point(0.0, 0.0));
                    //Point center = new Point(leftUpper.X + element.ActualWidth/2, leftUpper.Y + element.ActualHeight/2 );
                    String strCoordinates = coordinates.X + ":" + coordinates.Y;
                    response = Responder.CreateJsonResponse(ResponseStatus.UnknownError, strCoordinates);
                }
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
                    // DISPATCHER
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

        private void GetElementCoordinates(FrameworkElement element)
        {
            EventWaitHandle wait = new AutoResetEvent(false);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                
                var point = element.TransformToVisual(visualRoot).Transform(new Point(0, 0));
                Point center = new Point(point.X + (int)element.ActualWidth/2, point.Y - (int)element.ActualHeight/2);
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

        //ugly search workaround
        private void FindElementByName(String elementName)
        {
            FrameworkElement element = null;
            //Used to wait until the element is actually added
            EventWaitHandle wait = new AutoResetEvent(false);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var grids = GetDescendants<Grid>(visualRoot);
                var topGrid = grids.First() as FrameworkElement;
                if (topGrid != null)
                {
                    element = topGrid.FindName(elementName) as FrameworkElement;
                    if (element != null)
                        webElements.Add(elementName, element);
                    wait.Set();
                }
            });
            wait.WaitOne();
        }

        private void FindElementByType(String typeName)
        {
            FrameworkElement element = null;
            //Used to wait until the element is actually added
            EventWaitHandle wait = new AutoResetEvent(false);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var elements = GetDescendantsOfTypeName(visualRoot, typeName);
                element = elements.First() as FrameworkElement;
                if (element != null)
                    webElements.Add(typeName, element);
                wait.Set();
            });
            wait.WaitOne();
        }

        private IEnumerable<DependencyObject> GetDescendants(DependencyObject item)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(item);
            List<DependencyObject> children = new List<DependencyObject>();
            for (int i = 0; i < childrenCount; i++)
            {
                children.Add(VisualTreeHelper.GetChild(item, i));
            }

            foreach (var child in children)
            {
                yield return child;

                foreach (var grandChild in GetDescendants(child))
                {
                    yield return grandChild;
                }
            }
        }

        private IEnumerable<DependencyObject> GetDescendants<T>(DependencyObject item)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(item);
            List<DependencyObject> children = new List<DependencyObject>();
            for (int i = 0; i < childrenCount; i++)
            {
                children.Add(VisualTreeHelper.GetChild(item, i));
            }

            foreach (var child in children)
            {
                if (child is T)
                    yield return child;

                foreach (var grandChild in GetDescendants(child))
                {
                    if (grandChild is T)
                        yield return grandChild;
                }
            }
        }

        private IEnumerable<DependencyObject> GetDescendantsOfTypeName(DependencyObject item, String typeName)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(item);
            List<DependencyObject> children = new List<DependencyObject>();
            for (int i = 0; i < childrenCount; i++)
            {
                children.Add(VisualTreeHelper.GetChild(item, i));
            }

            foreach (var child in children)
            {
                if (child.GetType().ToString().Equals(typeName))
                    yield return child;

                foreach (var grandChild in GetDescendants(child))
                {
                    if (grandChild.GetType().ToString().Equals(typeName))
                        yield return grandChild;
                }
            }
        }


    }
}
