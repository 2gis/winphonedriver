namespace WindowsPhoneJsonWireServer
{
    using System;
    using System.Text;

    using Newtonsoft.Json;

    internal class Responder
    {
        #region Public Methods and Operators

        public static string CreateJsonResponse(ResponseStatus status, object value)
        {
            if (status != ResponseStatus.Success && value == null)
            {
                value = ErrorMessage(status);
            }

            return JsonConvert.SerializeObject(new JsonResponse(status, value));
        }

        public static string CreateResponse(string response)
        {
            var ret = new StringBuilder();
            ret.AppendLine("HTTP/1.1 200 OK");
            ret.AppendLine("Content-Type: application/json;charset=utf-8");
            ret.AppendLine("Connection: close");
            ret.AppendLine(string.Empty);
            ret.AppendLine(response);

            return ret.ToString();
        }

        public static string ErrorMessage(ResponseStatus status)
        {
            if (status == ResponseStatus.Success)
            {
                return null;
            }

            return string.Format("WebDriverException {0}", Enum.GetName(typeof(ResponseStatus), status));
        }

        #endregion
    }
}
