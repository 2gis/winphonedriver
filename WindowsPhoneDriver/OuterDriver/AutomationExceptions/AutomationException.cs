using System;
using System.Runtime.Serialization;


namespace OuterDriver.AutomationExceptions
{
    [Serializable]
    public class AutomationException : Exception
    {
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
    }
}