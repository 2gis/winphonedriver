namespace WindowsPhoneDriver.OuterDriver
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

    using DriverCommand = WindowsPhoneDriver.Common.DriverCommand;

    public class Listener
    {
        #region Static Fields

        private static string urnPrefix;

        #endregion

        #region Fields

        private Dictionary<string, object> actualCapabilities;

        private IDeployer deployer;

        private DispatchTables dispatcher;

        private EmulatorController emulatorController;

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

        public Point? RequestElementLocation(string element)
        {
            var command = new Command(
                null, 
                DriverCommand.GetElementLocation, 
                new Dictionary<string, object> { { "ID", element } });

            var responseBody = this.phoneRequester.ForwardCommand(command);

            var deserializeObject = JsonConvert.DeserializeObject<JsonResponse>(responseBody);

            if (deserializeObject.Status != ResponseStatus.Success)
            {
                return null;
            }

            var locationObject = deserializeObject.Value as JObject;
            if (locationObject == null)
            {
                return null;
            }

            var location = locationObject.ToObject<Dictionary<string, int>>();

            if (location == null)
            {
                return null;
            }

            var x = location["x"];
            var y = location["y"];
            return new Point(x, y);
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

        private static T GetValue<T>(IReadOnlyDictionary<string, object> parameters, string key) where T : class
        {
            object valueObject;
            parameters.TryGetValue(key, out valueObject);

            return valueObject as T;
        }

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
                                                { "deviceName", string.Empty }, 
                                                { "launchDelay", 0 }, 
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

            Console.WriteLine("Actual Device: " + this.deployer.DeviceName);
            this.emulatorController = new EmulatorController(this.deployer.DeviceName);

            return this.emulatorController.GetIpAddress();
        }

        private string ProcessCommand(Command command)
        {
            var responseBody = string.Empty;

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

                    long timeout = Convert.ToInt32(this.actualCapabilities["launchTimeout"]);
                    const int PingStep = 500;
                    var stopWatch = new Stopwatch();
                    while (timeout > 0)
                    {
                        stopWatch.Restart();
                        Console.Write(".");
                        var pingCommand = new Command(null, "ping", null);
                        responseBody = this.phoneRequester.ForwardCommand(pingCommand, false, 2000);
                        if (responseBody.StartsWith("<pong>"))
                        {
                            break;
                        }

                        Thread.Sleep(PingStep);
                        stopWatch.Stop();
                        timeout -= stopWatch.ElapsedMilliseconds;
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
                    var phoneScreenSize = this.emulatorController.PhoneScreenSize();
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
                    responseBody = this.emulatorController.TakeScreenshot();
                }
                else if (command.Name.Equals(DriverCommand.SendKeysToElement))
                {
                    // if the text has the ENTER command in it, execute it after sending the rest of the text to the inner driver
                    var needToClickEnter = false;
                    var originalContent = command.Parameters;
                    var value = ((object[])originalContent["value"]).Select(o => o.ToString()).ToArray();

                    const string EnterKey = "\ue007";

                    if (value.Contains(EnterKey))
                    {
                        needToClickEnter = true;
                        value = value.Where(val => val != EnterKey).ToArray();
                    }

                    command.Parameters["value"] = value;
                    responseBody = this.phoneRequester.ForwardCommand(command);

                    if (needToClickEnter)
                    {
                        this.emulatorController.PressEnterKey();
                    }
                }
                else if (command.Name.Equals(DriverCommand.MouseMoveTo))
                {
                    object elementId;
                    command.Parameters.TryGetValue("element", out elementId);

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
                        var xOffset = command.Parameters["xoffset"].ToString();
                        var yOffset = command.Parameters["yoffset"].ToString();
                        coordinates = new Point(int.Parse(xOffset), int.Parse(yOffset));
                    }

                    this.emulatorController.MoveCursorTo(coordinates);
                }
                else if (command.Name.Equals(DriverCommand.MouseClick))
                {
                    this.emulatorController.LeftButtonClick();
                }
                else if (command.Name.Equals(DriverCommand.ClickElement))
                {
                    var location = this.RequestElementLocation(command.Parameters["ID"] as string);

                    if (location.HasValue)
                    {
                        this.emulatorController.LeftButtonClick(location.Value);
                    }
                }
                else if (command.Name.Equals(DriverCommand.MouseDown))
                {
                    this.emulatorController.LeftButtonDown();
                }
                else if (command.Name.Equals(DriverCommand.MouseUp))
                {
                    this.emulatorController.LeftButtonUp();
                }
                else if (command.Name.Equals(DriverCommand.TouchFlick))
                {
                    var screen = this.emulatorController.PhoneScreenSize();
                    var startPoint = new Point(screen.Width / 2, screen.Height / 2);

                    var elementId = GetValue<string>(command.Parameters, "element");
                    if (elementId != null)
                    {
                        startPoint = this.RequestElementLocation(elementId).GetValueOrDefault();
                    }

                    object speed;
                    if (command.Parameters.TryGetValue("speed", out speed))
                    {
                        var xOffset = Convert.ToInt32(command.Parameters["xoffset"]);
                        var yOffset = Convert.ToInt32(command.Parameters["yoffset"]);

                        this.emulatorController.PerformGesture(
                            new FlickGesture(startPoint, xOffset, yOffset, Convert.ToDouble(speed)));
                    }
                    else
                    {
                        var xSpeed = Convert.ToDouble(command.Parameters["xspeed"]);
                        var ySpeed = Convert.ToDouble(command.Parameters["yspeed"]);
                        this.emulatorController.PerformGesture(new FlickGesture(startPoint, xSpeed, ySpeed));
                    }
                }
                else if (command.Name.Equals(DriverCommand.TouchScroll))
                {
                    var screen = this.emulatorController.PhoneScreenSize();
                    var startPoint = new Point(screen.Width / 2, screen.Height / 2);

                    var elementId = GetValue<string>(command.Parameters, "element");
                    if (elementId != null)
                    {
                        startPoint = this.RequestElementLocation(elementId).GetValueOrDefault();
                    }

                    // TODO: Add handling of missing parameters. Server should respond with a 400 Bad Request if parameters are missing
                    var xOffset = Convert.ToInt32(command.Parameters["xoffset"]);
                    var yOffset = Convert.ToInt32(command.Parameters["yoffset"]);

                    this.emulatorController.PerformGesture(new ScrollGesture(startPoint, xOffset, yOffset));
                }
                else if (command.Name.Equals(DriverCommand.TouchSingleTap))
                {
                    var elementId = GetValue<string>(command.Parameters, "element");
                    if (elementId != null)
                    {
                        var tapPoint = this.RequestElementLocation(elementId).GetValueOrDefault();
                        this.emulatorController.PerformGesture(new TapGesture(tapPoint));
                    }
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
