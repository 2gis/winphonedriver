namespace OuterDriver.AutomationExceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class AutomationException : Exception
    {
        #region Constructors and Destructors

        public AutomationException()
        {
        }

        public AutomationException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        public AutomationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AutomationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}
