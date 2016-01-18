namespace Winium.Mobile.Connectivity
{
    using System;

    public class ConnectivityException : Exception
    {
        #region Constructors and Destructors

        public ConnectivityException()
        {
        }

        public ConnectivityException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        public ConnectivityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion

    }
}
