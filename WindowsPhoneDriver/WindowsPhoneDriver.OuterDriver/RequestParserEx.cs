namespace WindowsPhoneDriver.OuterDriver
{
    using System.Collections.Generic;

    using WindowsPhoneDriver.Common;

    internal class RequestParserEx : RequestParser
    {
        #region Static Fields

        private static readonly List<string> CommandsToProxy = new List<string>
                                                                   {
                                                                       "element", 
                                                                       "elements", 
                                                                       "text", 
                                                                       "displayed", 
                                                                       "location", 
                                                                       "accept_alert", 
                                                                       "dismiss_alert", 
                                                                       "alert_text"
                                                                   };

        private static readonly List<string> CommandsWithGet = new List<string>
                                                                   {
                                                                       "text", 
                                                                       "displayed", 
                                                                       "location", 
                                                                       "alert_text"
                                                                   };

        #endregion

        #region Public Methods and Operators

        public static string ChooseRequestMethod(string uri)
        {
            return CommandsWithGet.Contains(RequestParser.GetUrnLastToken(uri)) ? "GET" : "POST";
        }

        public static bool ShouldProxyUrn(string urn)
        {
            return CommandsToProxy.Contains(RequestParser.GetUrnLastToken(urn));
        }

        #endregion
    }
}
