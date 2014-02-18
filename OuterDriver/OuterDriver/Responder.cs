using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OuterDriver
{
    class Responder
    {

        public static void WriteResponse(NetworkStream stream, String responseBody)
        {
            //the stream is closed in the calling method
            String response = CreateResponse(responseBody);
            var writer = new StreamWriter(stream);
            writer.Write(response);
            writer.Close();
        }

        private static String CreateResponse(String body)
        {
            StringBuilder responseString = new StringBuilder();
            responseString.AppendLine("HTTP/1.0 200 OK");
            responseString.AppendLine("Content-Type: application/json");
            responseString.AppendLine("Connection: close");
            responseString.AppendLine("");
            responseString.AppendLine(body);
            return responseString.ToString();
        }

    }
}
