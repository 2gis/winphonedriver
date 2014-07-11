namespace WindowsPhoneDriver.Common
{
    using System;
    using System.Linq;

    using Newtonsoft.Json;

    public class RequestParser
    {
        #region Static Fields

        private static string urnPrefix;

        #endregion

        #region Public Properties

        public static string UrnPrefix
        {
            get
            {
                return urnPrefix;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Normalize prefix
                    urnPrefix = "/" + value.Trim('/');
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        public static string GetElementId(string urn)
        {
            var urnTokens = GetUrnTokens(urn);
            return urnTokens.Length > 3 ? urnTokens[3] : null;
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
            if (!string.IsNullOrEmpty(UrnPrefix))
            {
                if (urn.StartsWith(UrnPrefix))
                {
                    urn = urn.Remove(0, UrnPrefix.Length);
                }
            }

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
