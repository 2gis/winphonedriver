using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;

namespace WindowsPhoneJsonWireServer
{
    class Automator
    {

        private Dictionary<String, FrameworkElement> webElements;
        private UIElement visualRoot;

        public Automator(UIElement visualRoot)
        {
            this.webElements = new Dictionary<string, FrameworkElement>();
            this.visualRoot = visualRoot;
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
                    {
                        webElements.Add(elementName, element);
                    }
                    wait.Set();
                }
            });
            wait.WaitOne();
        }

        public String PerformTextCommand(String elementId)
        {
            String text = String.Empty;
            String response = String.Empty;
            FrameworkElement element;
            if (webElements.TryGetValue(elementId, out element))
            {
                Button button = element as Button;
                if (button != null)
                {
                    TryClick(button);
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

        public String PerformElementCommand(FindElementObject elementObject)
        {
            String response = String.Empty;
            //search for the element by it's name
            //TODO - other search policies
            String elementName = elementObject.getValue();
            if (webElements.ContainsKey(elementName))
            {
                var webElement = new WebElement(elementName);
                response = Responder.CreateJsonResponse(0, webElement);
            }
            else
            {
                FindElementByName(elementName);
                //if the element has been sucessfully added
                if (webElements.ContainsKey(elementName))
                {
                    var webElement = new WebElement(elementName);
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
                    response = Responder.CreateJsonResponse(ResponseStatus.UnknownError, null);
                }
            }
            else
            {
                response = Responder.CreateJsonResponse(ResponseStatus.NoSuchElement, null);
            }
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

        // public 

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


    }
}
