namespace WindowsPhoneJsonWireServer
{
    using System;
    using System.Linq;

    using Newtonsoft.Json;

    internal class Parser
    {
        #region Public Methods and Operators

        public static string GetElementId(string urn)
        {
            var urnTokens = GetUrnTokens(urn);
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

        public static string GetRequestUrn(string request)
        {
            var firstHeaderTokens = request.Split(' ');
            var urn = firstHeaderTokens[1];
            return urn;
        }

        public static string GetUrnLastToken(string urn)
        {
            var urnTokens = GetUrnTokens(urn);
            var command = urnTokens[urnTokens.Length - 1];
            return command;
        }

        public static string[] GetUrnTokens(string urn)
        {
            var urnTokens = urn.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            return urnTokens;
        }

        public static int GetUrnTokensCount(string urn)
        {
            return GetUrnTokens(urn).Length;
        }

        #endregion
    }
}
