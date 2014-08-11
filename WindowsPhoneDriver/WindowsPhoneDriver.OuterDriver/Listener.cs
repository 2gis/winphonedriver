namespace WindowsPhoneDriver.OuterDriver
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using OpenQA.Selenium.Remote;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.OuterDriver.EmulatorHelpers;

    public class Listener
    {
        #region Static Fields

        private static string urnPrefix;

        #endregion

        #region Fields

        private Dictionary<string, object> actualCapabilities;

        private IDeployer deployer;

        private DispatchTables dispatcher;

        private EmulatorInputController inputController;

        private TcpListener listener;

        private Requester phoneRequester;

        private string sessionId;

        #endregion

        #region Constructors and Destructors

        public Listener(int listenerPort)
        {
            this.Port = listenerPort;
        }

        #endregion

        #region Public Properties

        public static string UrnPrefix
        {
            get
            {
                return urnPrefix;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Normalize prefix
                    urnPrefix = "/" + value.Trim('/');
                }
            }
        }

        public int Port { get; private set; }

        public Uri Prefix { get; private set; }

        #endregion

        #region Public Methods and Operators

        public static string FindIpAddress()
        {
            var localIp = "localhost";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            {
                localIp = ip.ToString();
            }

            return localIp;
        }

        public void StartListening()
        {
            try
            {
                this.listener = new TcpListener(IPAddress.Any, this.Port);

                this.Prefix = new Uri(string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}", this.Port));
                this.dispatcher = new DispatchTables(new Uri(this.Prefix, UrnPrefix));

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
                    var statusCode = HttpStatusCode.OK;
                    if (responseBody.Trim().Equals("<UnimplementedCommand>"))
                    {
                        statusCode = HttpStatusCode.NotImplemented;
                    }
                    else if (responseBody.Trim().Equals("<UnknownCommand>"))
                    {
                        statusCode = HttpStatusCode.NotFound;
                    }

                    Responder.WriteResponse(stream, responseBody, statusCode);

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
            /* Parses JSON and returns dictionary of supported capabilities and their values (or default values if not set)
             * launchTimeout - App launch timeout (app is pinged every 0.5 sec within launchTimeout;
             * launchDelay - gives time for visuals to initialize after app launch (successful ping)
             * reaching timeout will not raise any error, it will still wait for launchDelay and try to execute next ocmmand
            */
            var supportedCapabilities = new Dictionary<string, object>
                                            {
                                                { "app", string.Empty }, 
                                                { "platform", "WinPhone" }, 
                                                { "emulatorMouseDelay", 0 }, 
                                                { "deviceName", string.Empty }, 
                                                { "launchDelay", 1000 }, 
                                                { "launchTimeout", 10000 }, 
                                                { "debugConnectToRunningApp", "false" }, 
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

        private string HandleRequest(AcceptedRequest acceptedRequest)
        {
            Command commandToExecute;
            var request = acceptedRequest.Request;
            var content = acceptedRequest.Content;

            var firstHeaderTokens = request.Split(' ');
            var method = firstHeaderTokens[0];
            var resourcePath = firstHeaderTokens[1];

            try
            {
                var matched = this.dispatcher.Match(method, new Uri(this.Prefix, resourcePath));

                var commandName = matched.Data.ToString();
                commandToExecute = new Command(commandName, content);
                foreach (string variableName in matched.BoundVariables.Keys)
                {
                    commandToExecute.Parameters[variableName] = matched.BoundVariables[variableName];
                }
            }
            catch (UriTemplateMatchException)
            {
                return "<UnknownCommand>";
            }

            return this.ProcessCommand(commandToExecute);
        }

        private string InitializeApplication(bool debugDoNotDeploy = false)
        {
            var appPath = this.actualCapabilities["app"].ToString();
            this.deployer = new Deployer81(this.actualCapabilities["deviceName"].ToString());
            if (!debugDoNotDeploy)
            {
                this.deployer.Deploy(appPath);
            }

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
                ip = IPAddress.Parse(FindIpAddress()).ToString();
            }

            return ip;
        }

        private string ProcessCommand(Command command)
        {
            var responseBody = string.Empty;
            const string EnterKey = "\ue007";
            this.sessionId = "awesomeSessionId";
            try
            {
                if (command.Name.Equals(DriverCommand.NewSession))
                {
                    this.actualCapabilities = ParseDesiredCapabilitiesJson(command.ParametersAsJsonString);
                    var debugConnectToRunningApp = Convert.ToBoolean(
                        this.actualCapabilities["debugConnectToRunningApp"]);
                    var innerIp = this.InitializeApplication(debugConnectToRunningApp);
                    const int InnerPort = 9998;
                    Console.WriteLine("Inner ip: " + innerIp);
                    this.phoneRequester = new Requester(innerIp, InnerPort);

                    var timeout = Convert.ToInt32(this.actualCapabilities["launchTimeout"]);
                    const int PingStep = 500;
                    while (timeout > 0)
                    {
                        Console.Write(".");
                        timeout -= PingStep;
                        var pingCommand = new Command(null, "ping", null);
                        responseBody = this.phoneRequester.ForwardCommand(pingCommand, false);
                        if (responseBody.StartsWith("<pong>"))
                        {
                            break;
                        }
                        
                        Thread.Sleep(PingStep);
                    }
                    Console.WriteLine();
                    Thread.Sleep(Convert.ToInt32(this.actualCapabilities["launchDelay"]));

                    // gives sometime to load visuals
                    var jsonResponse = Responder.CreateJsonResponse(
                        this.sessionId, 
                        ResponseStatus.Success, 
                        this.actualCapabilities);
                    responseBody = jsonResponse;
                }
                else if (command.Name.Equals(DriverCommand.GetCurrentWindowHandle))
                {
                    // TODO: Is it temporary implementation? There is only one window for windows phone app, so it must be OK
                    responseBody = "current";
                }
                else if (command.Name.Equals(DriverCommand.GetWindowSize))
                {
                    // Window size is partially implemented
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
                else if (command.Name.Equals(DriverCommand.Screenshot))
                {
                    responseBody = ScreenShoter.TakeScreenshot();
                }
                else if (command.Name.Equals(DriverCommand.SendKeysToElement))
                {
                    // if the text has the ENTER command in it, execute it after sending the rest of the text to the inner driver
                    var needToClickEnter = false;
                    var originalContent = command.Parameters;
                    var value = ((object[])originalContent["value"]).Select(o => o.ToString()).ToArray();

                    if (value.Contains(EnterKey))
                    {
                        needToClickEnter = true;
                        value = value.Where(val => val != EnterKey).ToArray();
                    }

                    command.Parameters["value"] = value;
                    responseBody = this.phoneRequester.ForwardCommand(command);

                    if (needToClickEnter)
                    {
                        this.inputController.ClickEnterKey();
                    }
                }
                else if (command.Name.Equals(DriverCommand.MouseMoveTo))
                {
                    var elementId = command.Parameters["element"];

                    var coordinates = new Point();
                    if (elementId != null)
                    {
                        var parameters = new Dictionary<string, object> { { "ID", elementId } };
                        var locationCommand = new Command(null, DriverCommand.GetElementLocation, parameters);

                        responseBody = this.phoneRequester.ForwardCommand(locationCommand);

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
                        var xOffset = command.Parameters["xOffset"].ToString();
                        var yOffset = command.Parameters["YOffset"].ToString();
                        coordinates = new Point(int.Parse(xOffset), int.Parse(yOffset));
                    }

                    this.inputController.MoveCursorToPhoneScreenAtPoint(coordinates);
                }
                else if (command.Name.Equals(DriverCommand.MouseClick))
                {
                    this.inputController.LeftClick();
                }
                else if (command.Name.Equals(DriverCommand.ClickElement))
                {
                    responseBody = this.phoneRequester.ForwardCommand(command);

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
                else if (command.Name.Equals(DriverCommand.MouseDown))
                {
                    this.inputController.LeftButtonDown();
                }
                else if (command.Name.Equals(DriverCommand.MouseUp))
                {
                    this.inputController.LeftButtonUp();
                }
                else if (command.Name.Equals(DriverCommand.Close))
                {
                    this.deployer.Disconnect();
                }
                else
                {
                    responseBody = this.phoneRequester.ForwardCommand(command);
                }
            }
            catch (AutomationException ex)
            {
                responseBody = Responder.CreateJsonResponse(this.sessionId, ex.Status, ex.Message);
            }

            return responseBody;
        }

        #endregion
    }
}
