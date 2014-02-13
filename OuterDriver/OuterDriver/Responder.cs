using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OuterDriver
{
    class Responder
    {

        public static void Respond(NetworkStream stream, String responseBody)
        {
            String response = CreateResponse(responseBody);
            byte[] msg = System.Text.Encoding.ASCII.GetBytes(response);
            stream.Write(msg, 0, msg.Length);
        }

        private static String CreateResponse(String body)
        {
            StringBuilder response = new StringBuilder();
            response.AppendLine("HTTP/1.0 200 OK");
            response.AppendLine("Content-Type: application/json");
            response.AppendLine("Connection: close");
            response.AppendLine("");
            response.AppendLine(body);
            return response.ToString();
        }

    }
}
