using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms.VisualStyles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OuterDriver.AutomationExceptions;
using OuterDriver.EmulatorHelpers;

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
                Console.WriteLine("Request: " + request);

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
                    Console.WriteLine("Content: " + content);
                }
                return content;
            }

            private Dictionary<String, String> ReadHeaders(NetworkStream stream)
            {
                var headers = new Dictionary<String, String>();
                String header;
                while (!String.IsNullOrEmpty(header = ReadString(stream)))
                {
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
                    if (nextChar == '\n')
                    {
                        break;
                    }
                    if (nextChar == '\r')
                    {
                        continue;
                    }
                    data += Convert.ToChar(nextChar);
                }
                return data;
            }
        }

        private TcpListener _listener;
        private Requester _phoneRequester;
        private readonly int _listeningPort;

        private EmulatorInputController _inputController;
        private IDeployer _deployer;
        private Dictionary<string, object> _desiredCapabilities;
        private IPAddress _localAddr;

        public Listener(int listeningPort)
        {
            this._listeningPort = listeningPort;
        }

        public IPAddress IpAddress()
        {
            var endPoint = (IPEndPoint) _listener.LocalEndpoint;
            return endPoint.Address;
        }

        public int Port()
        {
            return _listeningPort;
        }

        public void StartListening()
        {
            try
            {
                _localAddr = IPAddress.Parse(OuterServer.FindIpAddress());
                _listener = new TcpListener(_localAddr, this._listeningPort);

                // Start listening for client requests.
                _listener.Start();

                // Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests. 
                    TcpClient client = _listener.AcceptTcpClient();

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
                _listener.Stop();
            }
        }

        private String HandleRequest(AcceptedRequest acceptedRequest)
        {
            String responseBody = String.Empty;
            String request = acceptedRequest.request;
            String content = acceptedRequest.content;
            if (Parser.ShouldProxy(request))
                responseBody = _phoneRequester.SendRequest(Parser.GetRequestUrn(request), content);
            else
                responseBody = HandleLocalRequest(acceptedRequest);

            return responseBody;
        }

        private String HandleLocalRequest(AcceptedRequest acceptedRequest)
        {
            String responseBody = String.Empty;
            String jsonValue = String.Empty;
            String ENTER = "\ue007";
            int innerPort = 9998;
            String sessionId = "awesomeSessionId";
            String request = acceptedRequest.request;
            String content = acceptedRequest.content;
            String command = Parser.GetRequestCommand(request);
            try
            {
                switch (command)
                {
                    case "session":
                        _desiredCapabilities = ParseDesiredCapabilitiesJson(content);
                        var innerIp = InitializeApplication();


                        Console.WriteLine("Inner ip: " + innerIp);
                        _phoneRequester = new Requester(innerIp, innerPort);
                        var jsonResponse = Responder.CreateJsonResponse(sessionId,
                            ResponseStatus.Sucess, _desiredCapabilities);
                        responseBody = jsonResponse;
                        break;
                    case "window_handle":
                        // TODO: Is it temporary implementation? There is only one window for windows phone app, so it must be OK
                        responseBody = "current";
                        break;
                    case "size":
                        // Window size is partially implemented
                        // TODO: Handle windows handles? 
                        var tokens = Parser.GetUrnTokens(request);
                        if (tokens.Length == 5 && tokens[2].Equals("window"))
                        {
                            var phoneScreenSize = _inputController.PhoneScreenSize();
                            responseBody = Responder.CreateJsonResponse(sessionId, ResponseStatus.Sucess,
                                new Dictionary<string, int>
                                {
                                    {"width", phoneScreenSize.Width},
                                    {"height", phoneScreenSize.Height}
                                });
                        }
                        else
                        {
                            goto default;
                        } // We can do better than goto

                        break;

                        //if the text has the ENTER command in it, execute it after sending the rest of the text to the inner driver
                    case "value":
                        bool needToClickEnter = false;
                        JsonValueContent oldContent = JsonConvert.DeserializeObject<JsonValueContent>(content);
                        String[] value = oldContent.GetValue();
                        if (value.Contains(ENTER))
                        {
                            needToClickEnter = true;
                            value = value.Where(val => val != ENTER).ToArray();
                        }
                        JsonValueContent newContent = new JsonValueContent(oldContent.sessionId, oldContent.id, value);
                        responseBody = _phoneRequester.SendRequest(Parser.GetRequestUrn(request),
                            JsonConvert.SerializeObject(newContent));
                        if (needToClickEnter)
                        {
                            _inputController.ClickEnterKey();
                        }
                        break;

                    case "moveto":
                        JsonMovetoContent moveToContent = JsonConvert.DeserializeObject<JsonMovetoContent>(content);
                        String elementId = moveToContent.element;
                        Point coordinates = new Point();
                        if (elementId != null)
                        {
                            String locationRequest = "/session/" + sessionId + "/element/" + elementId + "/location";
                            String locationResponse = _phoneRequester.SendRequest(locationRequest, String.Empty);
                            JsonResponse JsonResponse = JsonConvert.DeserializeObject<JsonResponse>(locationResponse);
                            Dictionary<String, String> values =
                                JsonConvert.DeserializeObject<Dictionary<String, String>>(JsonResponse.value.ToString());
                            coordinates.X = Convert.ToInt32(values["x"]);
                            coordinates.Y = Convert.ToInt32(values["y"]);
                        }
                        else
                        {
                            coordinates = new Point(Int32.Parse(moveToContent.xOffset),
                                Int32.Parse(moveToContent.yOffset));
                        }
                        _inputController.MoveCursorToPhoneScreenAtPoint(coordinates);
                        break;

                    case "click":
                        int requestLength = Parser.GetRequestLength(request);
                        if (requestLength == 3)
                        {
                            //simple click command without element
                            _inputController.LeftClick();
                            break;
                        }
                        responseBody = _phoneRequester.SendRequest(Parser.GetRequestUrn(request), content);
                        JsonResponse response = JsonConvert.DeserializeObject<JsonResponse>(responseBody);
                        var clickValue = (String) response.value;
                        if (clickValue != null)
                        {
                            String[] clickCoordinatesArray = ((String) clickValue).Split(':');
                            var xOffset = Convert.ToInt32(clickCoordinatesArray[0]);
                            var yOffset = Convert.ToInt32(clickCoordinatesArray[1]);
                            var point = new Point(xOffset, yOffset);
                            _inputController.LeftClickPhoneScreenAtPoint(point);
                            Console.WriteLine("Coordinates: " + xOffset + " " + yOffset);
                            responseBody = String.Empty;
                        }
                        break;

                    case "buttondown":
                        _inputController.LeftButtonDown();
                        break;

                    case "buttonup":
                        _inputController.LeftButtonUp();
                        break;

                    case "keys":
                        jsonValue = Parser.GetKeysString(content);
                        if (jsonValue.Equals(ENTER))
                        {
                            _inputController.ClickEnterKey();
                        }
                        break;

                    default:
                        Console.WriteLine("Not proxying. Unimplemented");
                        responseBody = "Success";
                        break;
                }
            }
            catch (MoveTargetOutOfBoundsException ex)
            {
                responseBody = Responder.CreateJsonResponse(sessionId,
                    ResponseStatus.MoveTargetOutOfBounds, ex.Message);
            }
            catch (AutomationException ex)
            {
                responseBody = Responder.CreateJsonResponse(sessionId,
                    ResponseStatus.UnknownError, ex.Message);
            }
            return responseBody;
        }

        private String InitializeApplication()
        {
            var appPath = _desiredCapabilities["app"].ToString();
            // TODO: There must be a way to get rid of hard coded app id
            const string appId = "69b4ce34-a3e0-414a-92d9-1302449f587c";
            _deployer = new Deployer81(_desiredCapabilities["deviceName"].ToString());
            _deployer.Deploy(appPath, appId, Convert.ToInt32(_desiredCapabilities["launchDelay"]));
            var ip = _deployer.ReceiveIpAddress();
            Console.WriteLine("Dev Name " + _deployer.DeviceName);
            _inputController = new EmulatorInputController(_deployer.DeviceName)
            {
                MouseMovmentSmoothing = Convert.ToInt32(_desiredCapabilities["emulatorMouseDelay"])
            };

            if (String.IsNullOrEmpty(ip))
            {
                ip = _localAddr.ToString();
            }

            return ip;
        }

        public void StopListening()
        {
            _listener.Stop();
        }

        private static Dictionary<string, object> ParseDesiredCapabilitiesJson(string content)
        {
            // Parses JSON and returns dictionary of supported capabilities and their values (or default values if not set)
            var supportedCapabilities = new Dictionary<string, object>()
            {
                {"app", string.Empty},
                {"platform", "WinPhone"},
                {"emulatorMouseDelay", 0},
                {"deviceName", string.Empty},
                {"launchDelay", 3500},
            };
            var actualCapabilities = new Dictionary<string, object>();
            var parsedContent = JObject.Parse(content);
            var dcToken = parsedContent["desiredCapabilities"];
            if (dcToken != null)
            {
                var desiredCapablilities = dcToken.ToObject<Dictionary<string, object>>();
                foreach (var capability in supportedCapabilities)
                {
                    object value = null;
                    if (!desiredCapablilities.TryGetValue(capability.Key, out value))
                    {
                        value = capability.Value;
                    }
                    actualCapabilities.Add(capability.Key, value);
                }
            }
            else
            {
                actualCapabilities = supportedCapabilities;
            }
            return actualCapabilities;
        }
    }
}