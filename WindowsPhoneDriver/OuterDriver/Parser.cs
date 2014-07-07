namespace OuterDriver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    internal class Parser
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
            return CommandsWithGet.Contains(GetLastToken(uri)) ? "GET" : "POST";
        }

        public static string GetKeysString(string requestContent)
        {
            var result = string.Empty;
            var jsonContent = JsonConvert.DeserializeObject<JsonKeysContent>(requestContent);
            var value = jsonContent.Value;
            return value.Aggregate(result, (current, str) => current + str);
        }

        public static string GetLastToken(string urn)
        {
            var urnTokens = SplitTokens(urn);
            var command = urnTokens[urnTokens.Length - 1];
            return command;
        }

        public static string GetRequestCommand(string request)
        {
            var tokens = GetUrnTokens(request);
            var command = tokens[tokens.Length - 1];
            return command;
        }

        // decides if the request should be simply proxied by looking at the last command token
        public static int GetRequestLength(string request)
        {
            return GetUrnTokens(request).Length;
        }

        public static string GetRequestUrn(string request)
        {
            var firstHeaderTokens = request.Split(' ');
            return firstHeaderTokens[1];
        }

        public static string[] GetUrnTokens(string request)
        {
            var urn = GetRequestUrn(request);
            return SplitTokens(urn);
        }

        public static bool ShouldProxy(string request)
        {
            var urn = GetRequestUrn(request);
            return CommandsToProxy.Contains(GetLastToken(urn));
        }

        #endregion

        #region Methods

        private static string[] SplitTokens(string urn)
        {
            var urnTokens = urn.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            return urnTokens;
        }

        #endregion

        // chooses the request method by looking at the last command token
    }
}
