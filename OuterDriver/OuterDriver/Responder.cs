using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Web.Script.Serialization;

namespace OuterDriver {

    class Responder {

        public static String CreateJsonResponse(String sessionId, ResponseStatus status, object jsonValue) {
            var serializer = new JavaScriptSerializer();
            var response = new JsonResponse(sessionId, status, serializer.Serialize(jsonValue));
            return serializer.Serialize(response);
        }

        public static void WriteResponse(NetworkStream stream, String responseBody) {
            //the stream is closed in the calling method
            try {
                String response = CreateResponse(responseBody);
                var writer = new StreamWriter(stream);
                writer.Write(response);
                writer.Close();
            }
            catch (Exception ex) {
                Console.WriteLine("Exception occurred " + ex.Message);
            }
        }

        private static String CreateResponse(String body) {
            StringBuilder responseString = new StringBuilder();
            responseString.AppendLine("HTTP/1.1 200 OK");
            responseString.AppendLine("Content-Type: application/json;charset=UTF-8");
            responseString.AppendLine("Connection: close");
            responseString.AppendLine("");
            responseString.AppendLine(body);
            return responseString.ToString();
        }

    }
}
