using System;
using System.Runtime.Serialization;


namespace OuterDriver.AutomationExceptions
{
    [Serializable]
    public class MoveTargetOutOfBoundsException : Exception
    {
        public MoveTargetOutOfBoundsException()
        {
        }

        public MoveTargetOutOfBoundsException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        public MoveTargetOutOfBoundsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MoveTargetOutOfBoundsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}