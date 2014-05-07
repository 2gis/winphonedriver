using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace OuterDriver {

    public class Listener {

        private class AcceptedRequest {
            public String request { get; set; }
            public Dictionary<String, String> headers { get; set; }
            public String content { get; set; }

            public void AcceptRequest(NetworkStream stream) {
                //read HTTP request
                this.request = ReadString(stream);
                Console.WriteLine("Request: " + request);

                //read HTTP headers
                this.headers = ReadHeaders(stream);

                //try and read request content
                this.content = ReadContent(stream, headers);
            }

            private string ReadContent(NetworkStream stream, Dictionary<String, String> headers) {
                String contentLengthString;
                bool hasContentLength = headers.TryGetValue("Content-Length", out contentLengthString);
                String content = "";
                if (hasContentLength) {
                    content = ReadContent(stream, Convert.ToInt32(contentLengthString));
                    Console.WriteLine("Content: " + content);
                }
                return content;
            }

            private Dictionary<String, String> ReadHeaders(NetworkStream stream) {
                var headers = new Dictionary<String, String>();
                String header;
                while (!String.IsNullOrEmpty(header = ReadString(stream))) {
                    String[] splitHeader;
                    splitHeader = header.Split(':');
                    headers.Add(splitHeader[0], splitHeader[1].Trim(' '));
                }
                return headers;
            }

            //reads the content of a request depending on its length
            private String ReadContent(NetworkStream s, int contentLength) {
                Byte[] readBuffer = new Byte[contentLength];
                int readBytes = s.Read(readBuffer, 0, readBuffer.Length);
                return System.Text.Encoding.ASCII.GetString(readBuffer, 0, readBytes);
            }

            private String ReadString(NetworkStream stream) {
                //StreamReader reader = new StreamReader(stream);
                int nextChar;
                String data = "";
                while (true) {
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

        public Listener(int listeningPort) {
            this.listeningPort = listeningPort;
        }

        public void StartListening() {
            try {
                IPAddress localAddr = IPAddress.Parse(OuterServer.FindIPAddress());
                listener = new TcpListener(localAddr, this.listeningPort);

                // Start listening for client requests.
                listener.Start();

                // Enter the listening loop
                while (true) {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests. 
                    TcpClient client = listener.AcceptTcpClient();

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
            catch (SocketException ex) {
                Console.WriteLine("SocketException: {0}", ex);
            }
            finally {
                // Stop listening for new clients.
                listener.Stop();
            }
        }

        private String HandleRequest(AcceptedRequest acceptedRequest) {
            String responseBody = String.Empty;
            String request = acceptedRequest.request;
            String content = acceptedRequest.content;
            if (Parser.ShouldProxy(request))
                responseBody = phoneRequester.SendRequest(Parser.GetRequestUrn(request), content);
            else
                responseBody = HandleLocalRequest(acceptedRequest);

            return responseBody;
        }

        private String HandleLocalRequest(AcceptedRequest acceptedRequest) {
            String responseBody = String.Empty;
            String jsonValue = String.Empty;
            String ENTER = "\ue007";
            int innerPort = 9998;
            String request = acceptedRequest.request;
            String content = acceptedRequest.content;
            String command = Parser.GetRequestCommand(request);
            switch (command) {
                case "session":
                    String sessionId = "awesomeSessionId";
                    //String innerIp = InitializeApplication();
                    Console.WriteLine("Enter inner driver ip");
                    String innerIp = Console.ReadLine();
                    //Console.WriteLine("Inner ip: " + innerIp);
                    phoneRequester = new Requester(innerIp, innerPort);
                    String jsonResponse = Responder.CreateJsonResponse(sessionId,
                        ResponseStatus.Sucess, new JsonCapabilities("WinPhone"));
                    responseBody = jsonResponse;
                    break;

                //if the text has the ENTER command in it, execute it after sending the rest of the text to the inner driver
                case "value":
                    bool needToClickEnter = false;
                    JsonValueContent oldContent = JsonConvert.DeserializeObject<JsonValueContent>(content);
                    String[] value = oldContent.GetValue();
                    if (value.Contains(ENTER)) {
                        needToClickEnter = true;
                        value = value.Where(val => val != ENTER).ToArray();
                    }
                    JsonValueContent newContent = new JsonValueContent(oldContent.sessionId, oldContent.id, value);
                    responseBody = phoneRequester.SendRequest(Parser.GetRequestUrn(request), JsonConvert.SerializeObject(newContent));
                    if (needToClickEnter)
                        OuterDriver.ClickEnter();
                    break;

                case "moveto":
                    JsonMovetoContent moveToContent = JsonConvert.DeserializeObject<JsonMovetoContent>(content);
                    var coordinates = new Point(Int32.Parse(moveToContent.xOffset), Int32.Parse(moveToContent.yOffset));
                    OuterDriver.MoveCursorToEmulatorCoordinates(coordinates);
                    break;

                case "click":

                    responseBody = phoneRequester.SendRequest(Parser.GetRequestUrn(request), content);
                    JsonResponse response = JsonConvert.DeserializeObject<JsonResponse>(responseBody);
                    var clickValue = (String)response.value;
                    if (clickValue != null) {
                        String[] clickCoordinatesArray = ((String)clickValue).Split(':');
                        int xOffset = Convert.ToInt32(clickCoordinatesArray[0]);
                        int yOffset = Convert.ToInt32(clickCoordinatesArray[1]);
                        Point point = new Point(xOffset, yOffset);
                        OuterDriver.ClickEmulatorScreenPoint(point);
                        Console.WriteLine("Coordinates: " + xOffset + " " + yOffset);
                        responseBody = String.Empty;
                    }
                    break;

                case "keys":
                    jsonValue = Parser.GetKeysString(content);
                    if (jsonValue.Equals(ENTER))
                        OuterDriver.ClickEnter();
                    break;

                default:
                    Console.WriteLine("Not proxying. Unimplemented");
                    responseBody = "Success";
                    break;
            }
            return responseBody;
        }

        private String InitializeApplication() {
            String appId = "846135ee-2c7a-453c-9a72-e57c607c26c8";
            var deployer = new Deployer(appId);
            deployer.Deploy();
            String ip = deployer.ReceiveIpAddress();
            return ip;

            return String.Empty;
        }

        public void StopListening() {
            listener.Stop();
        }

    }

}
