using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsPhoneJsonWireServer
{
    class Responder
    {

        private String sessionId = "awesomeSessionId";

        public static String CreateResponse(String response)
        {
            StringBuilder ret = new StringBuilder();
            ret.AppendLine("HTTP/1.1 200 OK");
            ret.AppendLine("Content-Type: application/json;charset=utf-8");
            ret.AppendLine("Connection: close");
            ret.AppendLine("");
            ret.AppendLine(response);

            return ret.ToString();
        }

        public static String CreateJsonResponse(int status, object value)
        {
            return JsonConvert.SerializeObject(new JsonResponse(status, value));
        }

    }
}
