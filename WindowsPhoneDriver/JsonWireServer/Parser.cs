namespace WindowsPhoneJsonWireServer
{
    using System;
    using System.Linq;

    using Newtonsoft.Json;

    internal class Parser
    {
        #region Public Methods and Operators

        public static string GetElementId(string request)
        {
            var requestTokens = request.Split(' ');
            var urn = requestTokens[1];

            // take care of the slash in the end of the string
            var urnTokens = urn.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            var elementId = urnTokens[urnTokens.Length - 2];
            return elementId;
        }

        public static string GetKeysString(string requestContent)
        {
            var result = string.Empty;
            var jsonContent = JsonConvert.DeserializeObject<JsonKeysContent>(requestContent);
            var value = jsonContent.Value;

            return value.Aggregate(result, (current, str) => current + str);
        }

        public static string GetRequestCommand(string request)
        {
            var requestTokens = request.Split(' ');
            var urn = requestTokens[1];

            // take care of the slash in the end of the string
            var urnTokens = urn.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            var command = urnTokens[urnTokens.Length - 1];
            return command;
        }

        public static int GetUrnTokensLength(string request)
        {
            var requestTokens = request.Split(' ');
            var urn = requestTokens[1];

            // take care of the slash in the end of the string
            var urnTokens = urn.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            return urnTokens.Length;
        }

        #endregion
    }
}
