using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web.Script.Serialization;

namespace OuterDriver
{

    public class Listener
    {

        private class AcceptedRequest
        {
            public String request { get; set; }
            public Dictionary<String, String> headers { get; set; }
            public String content { get; set; }

            public void AcceptRequest(NetworkStream stream)
            {
                //read HTTP request
                this.request = ReadString(stream);
                Console.WriteLine(request);

                //read HTTP headers
                this.headers = ReadHeaders(stream);

                //try and read request content
                this.content = ReadContent(stream, headers);
            }

            private string ReadContent(NetworkStream stream, Dictionary<String, String> headers)
            {
                String contentLengthString;
                bool hasContentLength = headers.TryGetValue("Content-Length", out contentLengthString);
                String content = "";
                if (hasContentLength)
                {
                    content = ReadContent(stream, Convert.ToInt32(contentLengthString));
                    Console.WriteLine(content);
                }
                return content;
            }

            private Dictionary<String, String> ReadHeaders(NetworkStream stream)
            {
                var headers = new Dictionary<String, String>();
                String header;
                while (!String.IsNullOrEmpty(header = ReadString(stream)))
                {
                    Console.WriteLine(header);
                    String[] splitHeader;
                    splitHeader = header.Split(':');
                    headers.Add(splitHeader[0], splitHeader[1].Trim(' '));
                }
                return headers;
            }

            //reads the content of a request depending on its length
            private String ReadContent(NetworkStream s, int contentLength)
            {
                Byte[] readBuffer = new Byte[contentLength];
                int readBytes = s.Read(readBuffer, 0, readBuffer.Length);
                return System.Text.Encoding.ASCII.GetString(readBuffer, 0, readBytes);
            }

            private String ReadString(NetworkStream stream)
            {
                //StreamReader reader = new StreamReader(stream);
                int nextChar;
                String data = "";
                while (true)
                {
                    nextChar = stream.ReadByte();
                    if (nextChar == '\n') { break; }
                    if (nextChar == '\r') { continue; }
                    data += Convert.ToChar(nextChar);
                }
                return data;
            }

        }

        private TcpListener listener;
        private Requester phoneRequester;
        private readonly int listeningPort;

        public Listener(int listeningPort, int phonePort, String phoneIp)
        {
            this.listeningPort = listeningPort;
            this.listener = null;
            phoneRequester = new Requester(phoneIp, phonePort);
        }

        public void StartListening()
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(OuterServer.FindIPAddress());
                listener = new TcpListener(localAddr, this.listeningPort);

                // Start listening for client requests.
                listener.Start();

                // Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests. 
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    var acceptedRequest = new AcceptedRequest();
                    acceptedRequest.AcceptRequest(stream);

                    String responseBody = HandleRequest(acceptedRequest);
                    
                    Responder.WriteResponse(stream, responseBody);

                    // Shutdown and end connection
                    stream.Close();
                    client.Close();

                    Console.WriteLine("Client closed\n");
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex);
            }
            finally
            {
                // Stop listening for new clients.
                listener.Stop();
            }
        }
  
        private String HandleRequest(AcceptedRequest acceptedRequest)
        {
            String responseBody = String.Empty;
            String request = acceptedRequest.request;
            String content = acceptedRequest.content;
            if (Parser.ShouldProxy(request))
            {
                Console.WriteLine("proxying");
                responseBody = phoneRequester.SendRequest(Parser.GetRequestUrn(request), content);
            }
            else
            {
                responseBody = HandleLocalRequest(acceptedRequest);
            }
            return responseBody;
        }

        private String HandleLocalRequest(AcceptedRequest acceptedRequest)
        {
            String responseBody = String.Empty;
            String request = acceptedRequest.request;
            String command = Parser.GetRequestCommand(request);
            switch (command)
            {
                case "session":
                    String jsonResponse = Responder.CreateJsonResponse("MyId",
                        ResponseStatus.Sucess, new JsonCapabilities("WinPhone"));
                    Console.WriteLine("jsonResponse: " + jsonResponse);
                    responseBody = jsonResponse;
                    break;

                default:
                    Console.WriteLine("Not proxying. Unimplemented");
                    responseBody = "Success";
                    break;
            }
            return responseBody;
        }

        public void StopListening()
        {
            listener.Stop();
        }

    }

}
