namespace WindowsPhoneDriver.Common.Exceptions
{
    using System;
    using System.Net;

    public class InnerDriverRequestError : Exception
    {
        #region Constructors and Destructors

        public InnerDriverRequestError()
        {
        }

        public InnerDriverRequestError(string message, HttpStatusCode statusCode)
            : base(message)
        {
            this.StatusCode = statusCode;
        }

        public InnerDriverRequestError(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        public InnerDriverRequestError(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion

        #region Public Properties

        public HttpStatusCode StatusCode { get; set; }

        #endregion
    }
}
