namespace OuterDriver
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;

    using Newtonsoft.Json;

    using WindowsPhoneDriver.Common;

    internal class Responder
    {
        #region Public Methods and Operators

        public static string CreateJsonResponse(string sessionId, ResponseStatus status, object jsonValue)
        {
            return JsonConvert.SerializeObject(new JsonResponse(sessionId, status, jsonValue));
        }

        public static void WriteResponse(NetworkStream stream, string responseBody)
        {
            // the stream is closed in the calling method
            try
            {
                var response = CreateResponse(responseBody);
                var writer = new StreamWriter(stream);
                writer.Write(response);
                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred " + ex.Message);
            }
        }

        #endregion

        #region Methods

        private static string CreateResponse(string body)
        {
            var responseString = new StringBuilder();
            responseString.AppendLine("HTTP/1.1 200 OK");
            responseString.AppendLine("Content-Type: application/json;charset=UTF-8");
            responseString.AppendLine("Connection: close");
            responseString.AppendLine(string.Empty);
            responseString.AppendLine(body);
            return responseString.ToString();
        }

        #endregion
    }
}
