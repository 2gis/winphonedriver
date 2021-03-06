﻿namespace WindowsPhoneDriver.InnerDriver.Commands
{
    using System;
    using System.Linq;

    using WindowsPhoneDriver.Common;

    internal class GetElementAttributeCommand : CommandBase
    {
        #region Public Properties

        public string ElementId { get; set; }

        #endregion

        #region Public Methods and Operators

        public override string DoImpl()
        {
            var element = this.Automator.WebElements.GetRegisteredElement(this.ElementId);

            object value;
            var attributeName = (string)null;
            if (this.Parameters.TryGetValue("NAME", out value))
            {
                attributeName = value.ToString();
            }

            if (attributeName == null)
            {
                return this.JsonResponse(ResponseStatus.Success, null);
            }

            //Support for attributes with chained names like "Control.RenderTransform.TranslateX"
            var finalValue = attributeName
                .Split('.')
                .Aggregate(
                    (object)element,
                    (currentValue, propertyName) =>
                        {
                            var prop = currentValue == null ? null: currentValue.GetType().GetProperty(propertyName);
                            return currentValue == null || prop == null ? null : prop.GetValue(currentValue);
                        });

            if (finalValue == null)
            {
                return this.JsonResponse(ResponseStatus.Success, null);
            }

            return this.JsonResponse(ResponseStatus.Success, finalValue.ToString());
        }

        #endregion
    }
}
