namespace WindowsPhoneDriver.OuterDriver
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.OuterDriver.EmulatorHelpers;

    public class Listener
    {
        #region Fields

        private readonly int listeningPort;

        private Dictionary<string, object> actualCapabilities;

        private IDeployer deployer;

        private EmulatorInputController inputController;

        private TcpListener listener;

        private IPAddress localAddress;

        private Requester phoneRequester;

        private string sessionId;

        #endregion

        #region Constructors and Destructors

        public Listener(int listeningPort)
        {
            this.listeningPort = listeningPort;
        }

        #endregion

        #region Public Methods and Operators

        public int Port()
        {
            return this.listeningPort;
        }

        public void StartListening()
        {
            try
            {
                this.localAddress = IPAddress.Parse(OuterServer.FindIpAddress());
                this.listener = new TcpListener(IPAddress.Any, this.listeningPort);

                // Start listening for client requests.
                this.listener.Start();

                // Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests. 
                    var client = this.listener.AcceptTcpClient();

                    // Get a stream object for reading and writing
                    var stream = client.GetStream();

                    var acceptedRequest = new AcceptedRequest();
                    acceptedRequest.AcceptRequest(stream);

                    var responseBody = this.HandleRequest(acceptedRequest);

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
                this.listener.Stop();
            }
        }

        public void StopListening()
        {
            this.listener.Stop();
        }

        #endregion

        #region Methods

        private static Dictionary<string, object> ParseDesiredCapabilitiesJson(string content)
        {
            // Parses JSON and returns dictionary of supported capabilities and their values (or default values if not set)
            var supportedCapabilities = new Dictionary<string, object>
                                            {
                                                { "app", string.Empty }, 
                                                { "platform", "WinPhone" }, 
                                                { "emulatorMouseDelay", 0 }, 
                                                { "deviceName", string.Empty }, 
                                                { "launchTimeout", 10000 }, 
                                                { "launchDelay", 1000 }, 
 
                                                // launchTimeout - How long will we wait for ping response from automator (app side) 
                                                // launchDelay - Lets give some time for visuals to appear after successful ping response
                                            };
            var actualCapabilities = new Dictionary<string, object>();
            var parsedContent = JObject.Parse(content);
            var desiredCapabilitiesToken = parsedContent["desiredCapabilities"];
            if (desiredCapabilitiesToken != null)
            {
                var desiredCapabilities = desiredCapabilitiesToken.ToObject<Dictionary<string, object>>();
                foreach (var capability in supportedCapabilities)
                {
                    object value;
                    if (!desiredCapabilities.TryGetValue(capability.Key, out value))
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

        private string HandleLocalRequest(AcceptedRequest acceptedRequest)
        {
            var responseBody = string.Empty;
            const string EnterKey = "\ue007";
            this.sessionId = "awesomeSessionId";
            var request = acceptedRequest.Request;
            var content = acceptedRequest.Content;
            var urn = RequestParser.GetRequestUrn(request);
            var command = RequestParser.GetUrnLastToken(urn);
            try
            {
                switch (command)
                {
                    case "session":
                        this.actualCapabilities = ParseDesiredCapabilitiesJson(content);
                        var innerIp = this.InitializeApplication();
                        const int InnerPort = 9998;
                        Console.WriteLine("Inner ip: " + innerIp);
                        this.phoneRequester = new Requester(innerIp, InnerPort);

                        this.WaitForApplicationToLaunch(Convert.ToInt32(this.actualCapabilities["launchTimeout"]));

                        // waits for successful ping
                        Thread.Sleep(Convert.ToInt32(this.actualCapabilities["launchDelay"]));

                        // gives sometime to load visuals
                        var jsonResponse = Responder.CreateJsonResponse(
                            this.sessionId, 
                            ResponseStatus.Success, 
                            this.actualCapabilities);
                        responseBody = jsonResponse;
                        break;

                    case "window_handle":

                        // TODO: Is it temporary implementation? There is only one window for windows phone app, so it must be OK
                        responseBody = "current";
                        break;

                    case "size":

                        // Window size is partially implemented
                        // TODO: Handle windows handles? 
                        var tokens = RequestParser.GetUrnTokens(urn);
                        if (tokens.Length == 5 && tokens[2].Equals("window"))
                        {
                            var phoneScreenSize = this.inputController.PhoneScreenSize();
                            responseBody = Responder.CreateJsonResponse(
                                this.sessionId, 
                                ResponseStatus.Success, 
                                new Dictionary<string, int>
                                    {
                                        { "width", phoneScreenSize.Width }, 
                                        { "height", phoneScreenSize.Height }
                                    });
                        }
                        else
                        {
                            // We can do better than goto
                            goto default;
                        }

                        break;

                    case "screenshot":
                        responseBody = ScreenShoter.TakeScreenshot();
                        break;

                        // if the text has the ENTER command in it, execute it after sending the rest of the text to the inner driver
                    case "value":
                        var needToClickEnter = false;
                        var oldContent = JsonConvert.DeserializeObject<JsonValueContent>(content);
                        var value = oldContent.Value;
                        if (value.Contains(EnterKey))
                        {
                            needToClickEnter = true;
                            value = value.Where(val => val != EnterKey).ToArray();
                        }

                        var newContent = new JsonValueContent(oldContent.SessionId, oldContent.Id, value);
                        responseBody = this.phoneRequester.SendRequest(
                            RequestParser.GetRequestUrn(request), 
                            JsonConvert.SerializeObject(newContent));
                        if (needToClickEnter)
                        {
                            this.inputController.ClickEnterKey();
                        }

                        break;

                    case "moveto":
                        {
                            var moveToContent = JsonConvert.DeserializeObject<JsonMovetoContent>(content);
                            var elementId = moveToContent.Element;
                            var coordinates = new Point();
                            if (elementId != null)
                            {
                                var locationRequest = "/session/" + this.sessionId + "/element/" + elementId
                                                      + "/location";
                                responseBody = this.phoneRequester.SendRequest(locationRequest, string.Empty);

                                var deserializeObject = JsonConvert.DeserializeObject<JsonResponse>(responseBody);
                                if (deserializeObject.Status == ResponseStatus.Success)
                                {
                                    var values =
                                        JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                            deserializeObject.Value.ToString());
                                    coordinates.X = Convert.ToInt32(values["x"]);
                                    coordinates.Y = Convert.ToInt32(values["y"]);
                                }
                            }
                            else
                            {
                                coordinates = new Point(
                                    int.Parse(moveToContent.XOffset), 
                                    int.Parse(moveToContent.YOffset));
                            }

                            this.inputController.MoveCursorToPhoneScreenAtPoint(coordinates);
                        }

                        break;

                    case "click":
                        var requestLength = RequestParser.GetUrnTokensCount(urn);
                        if (requestLength == 3)
                        {
                            // simple click command without element
                            this.inputController.LeftClick();
                        }
                        else
                        {
                            responseBody = this.phoneRequester.SendRequest(
                                RequestParser.GetRequestUrn(request), 
                                content);
                            var deserializeObject = JsonConvert.DeserializeObject<JsonResponse>(responseBody);
                            if (deserializeObject.Status == ResponseStatus.Success)
                            {
                                var clickValue = deserializeObject.Value.ToString();
                                if (!string.IsNullOrEmpty(clickValue))
                                {
                                    var clickCoordinatesArray = clickValue.Split(':');
                                    var offsetX = Convert.ToInt32(clickCoordinatesArray[0]);
                                    var offsetY = Convert.ToInt32(clickCoordinatesArray[1]);
                                    var point = new Point(offsetX, offsetY);
                                    this.inputController.LeftClickPhoneScreenAtPoint(point);
                                    Console.WriteLine("Coordinates: " + offsetX + " " + offsetY);
                                    responseBody = string.Empty;
                                }
                            }
                        }

                        break;

                    case "buttondown":
                        this.inputController.LeftButtonDown();
                        break;

                    case "buttonup":
                        this.inputController.LeftButtonUp();
                        break;

                    case "keys":
                        var jsonValue = RequestParser.GetKeysString(content);
                        if (jsonValue.Equals(EnterKey))
                        {
                            this.inputController.ClickEnterKey();
                        }

                        break;

                    default:
                        Console.WriteLine("Not proxying. Unimplemented");
                        responseBody = "Success";
                        break;
                }
            }
            catch (AutomationException ex)
            {
                responseBody = Responder.CreateJsonResponse(this.sessionId, ex.Status, ex.Message);
            }

            return responseBody;
        }

        private string HandleRequest(AcceptedRequest acceptedRequest)
        {
            var request = acceptedRequest.Request;
            var content = acceptedRequest.Content;
            var urn = RequestParser.GetRequestUrn(request);

            return RequestParserEx.ShouldProxyUrn(urn)
                       ? this.phoneRequester.SendRequest(urn, content)
                       : this.HandleLocalRequest(acceptedRequest);
        }

        private string InitializeApplication()
        {
            var appPath = this.actualCapabilities["app"].ToString();
            this.deployer = new Deployer81(this.actualCapabilities["deviceName"].ToString());

            this.deployer.Deploy(appPath);
            var ip = this.deployer.ReceiveIpAddress();
            Console.WriteLine("Actual Device: " + this.deployer.DeviceName);
            var desiredsmoothing = Convert.ToInt32(this.actualCapabilities["emulatorMouseDelay"]);
            this.inputController = new EmulatorInputController(this.deployer.DeviceName)
                                       {
                                           MouseMovementSmoothing =
                                               desiredsmoothing
                                       };

            if (string.IsNullOrEmpty(ip))
            {
                ip = this.localAddress.ToString();
            }

            return ip;
        }

        private void WaitForApplicationToLaunch(int timeout)
        {
            // Supposed to wait for application to launch, but ping response might come earlier than visual tree is build or something
            const int StepDelay = 500;
            var pingSuccess = false;
            while (timeout > 0)
            {
                Thread.Sleep(StepDelay);
                var urn = string.Format("/session/{0}/ping", this.sessionId);
                var pingResult = this.phoneRequester.SendRequest(urn, string.Empty);
                if (!string.IsNullOrEmpty(pingResult) && !pingResult.Equals("error"))
                {
                    pingSuccess = true;
                    break;
                }

                timeout -= StepDelay;
            }

            if (pingSuccess)
            {
                return;
            }

            Console.WriteLine("Timeout: App is not running. Use launchTimeout desired capability to increase timeout.");
            throw new AutomationException("Application is not running");
        }

        #endregion

        private class AcceptedRequest
        {
            #region Public Properties

            public string Content { get; private set; }

            public string Request { get; private set; }

            #endregion

            #region Properties

            private Dictionary<string, string> Headers { get; set; }

            #endregion

            #region Public Methods and Operators

            public void AcceptRequest(NetworkStream stream)
            {
                // read HTTP request
                this.Request = this.ReadString(stream);
                Console.WriteLine("Request: " + this.Request);

                // read HTTP headers
                this.Headers = this.ReadHeaders(stream);

                // try and read request content
                this.Content = this.ReadContent(stream, this.Headers);
            }

            #endregion

            #region Methods

            private string ReadContent(NetworkStream stream, Dictionary<string, string> headers)
            {
                string contentLengthString;
                var hasContentLength = headers.TryGetValue("Content-Length", out contentLengthString);
                var content = string.Empty;
                if (hasContentLength)
                {
                    content = this.ReadContent(stream, Convert.ToInt32(contentLengthString));
                    Console.WriteLine("Content: " + content);
                }

                return content;
            }

            // reads the content of a request depending on its length
            private string ReadContent(NetworkStream s, int contentLength)
            {
                var readBuffer = new byte[contentLength];
                var readBytes = s.Read(readBuffer, 0, readBuffer.Length);
                return System.Text.Encoding.ASCII.GetString(readBuffer, 0, readBytes);
            }

            private Dictionary<string, string> ReadHeaders(NetworkStream stream)
            {
                var headers = new Dictionary<string, string>();
                string header;
                while (!string.IsNullOrEmpty(header = this.ReadString(stream)))
                {
                    var splitHeader = header.Split(':');
                    headers.Add(splitHeader[0], splitHeader[1].Trim(' '));
                }

                return headers;
            }

            private string ReadString(NetworkStream stream)
            {
                // StreamReader reader = new StreamReader(stream);
                string data = string.Empty;
                while (true)
                {
                    var nextChar = stream.ReadByte();
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

            #endregion
        }
    }
}
