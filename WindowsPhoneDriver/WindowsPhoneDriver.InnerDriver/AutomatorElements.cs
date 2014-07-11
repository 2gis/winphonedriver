namespace WindowsPhoneDriver.InnerDriver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;

    using WindowsPhoneDriver.Common;

    internal class AutomatorElements
    {
        private static int safeInstanceCount;

        private readonly Dictionary<string, WeakReference> registeredElements;

        public AutomatorElements()
        {
            this.registeredElements = new Dictionary<string, WeakReference>();
        }

        public string RegisterElement(FrameworkElement element)
        {
            var registeredKey = this.registeredElements.FirstOrDefault(x => x.Value.Target == element).Key;

            if (registeredKey == null)
            {
                Interlocked.Increment(ref safeInstanceCount);

                registeredKey = element.GetHashCode() + "-" + safeInstanceCount.ToString(string.Empty);
                this.registeredElements.Add(registeredKey, new WeakReference(element));
            }

            return registeredKey;
        }

        /// <summary>
        /// Returns FrameworkElement registered with specified key if any exists. Throws if no element is found.
        /// </summary>
        /// <param name="registeredKey"></param>
        /// <exception cref="AutomationException">Registered element is not found or element has been garbage collected.</exception>
        /// <returns></returns>
        public FrameworkElement GetRegisteredElement(string registeredKey)
        {
            WeakReference reference;
            if (this.registeredElements.TryGetValue(registeredKey, out reference))
            {
                var item = reference.Target as FrameworkElement;
                if (item != null)
                {
                    return item;
                }
            }

            throw new AutomationException("Stale element reference", ResponseStatus.StaleElementReference);
        }
    }
}
