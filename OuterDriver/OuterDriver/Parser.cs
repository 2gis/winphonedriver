using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OuterDriver
{

    class Parser
    {

        private static readonly List<String> CommandsToProxy = new List<String> { "element", "elements", "text", "displayed", "location" };
        private static readonly List<String> CommandsWithGet = new List<String> { "text", "displayed", "location" };

        public static String GetRequestUrn(String request)
        {
            var firstHeaderTokens = request.Split(' ');
            return firstHeaderTokens[1];
        }

        public static String GetLastToken(String urn)
        {
            var urnTokens = SplitTokens(urn);
            var command = urnTokens[urnTokens.Length - 1];
            return command;
        }

        public static String GetRequestCommand(String request)
        {
            var tokens = GetUrnTokens(request);
            var command = tokens[tokens.Length - 1];
            return command;
        }

        //decides if the request should be simply proxied by looking at the last command token
        public static bool ShouldProxy(String request)
        {
            var urn = GetRequestUrn(request);
            return CommandsToProxy.Contains(GetLastToken(urn));
        }

        //deserializes json string array and returns a single String
        public static String GetKeysString(String requestContent)
        {
            var result = String.Empty;
            var jsonContent = JsonConvert.DeserializeObject<JsonKeysContent>(requestContent);
            var value = jsonContent.GetValue();
            return value.Aggregate(result, (current, str) => current + str);
        }

        public static int GetRequestLength(String request)
        {
            return GetUrnTokens(request).Length;
        }

        private static String[] SplitTokens(String urn)
        {
            var urnTokens = urn.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            return urnTokens;
        }

        public static String[] GetUrnTokens(String request)
        {
            var urn = GetRequestUrn(request);
            return SplitTokens(urn);
        }

        //chooses the request method by looking at the last command token
        public static String ChooseRequestMethod(String uri)
        {
            return CommandsWithGet.Contains(GetLastToken(uri)) ? "GET" : "POST";
        }
    }
}
