using Newtonsoft.Json;
using System;
using System.Text;

namespace WindowsPhoneJsonWireServer
{
    class Responder
    {

        private String sessionId = "awesomeSessionId";

        public static String CreateResponse(String response)
        {
            var ret = new StringBuilder();
            ret.AppendLine("HTTP/1.1 200 OK");
            ret.AppendLine("Content-Type: application/json;charset=utf-8");
            ret.AppendLine("Connection: close");
            ret.AppendLine("");
            ret.AppendLine(response);

            return ret.ToString();
        }

        public static String CreateJsonResponse(ResponseStatus status, object value)
        {
            if (status != ResponseStatus.Success && value == null)
            {
                value = ErrorMessage(status);
            }
            return JsonConvert.SerializeObject(new JsonResponse(status, value));
        }

        public static String ErrorMessage(ResponseStatus status)
        {
            if (status == ResponseStatus.Success)
                return null;
            return String.Format("WebDriverException {0}", Enum.GetName(typeof(ResponseStatus), status));
        }
    }
}
