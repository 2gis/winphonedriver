namespace OuterDriver.AutomationExceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class MoveTargetOutOfBoundsException : Exception
    {
        #region Constructors and Destructors

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

        #endregion
    }
}
