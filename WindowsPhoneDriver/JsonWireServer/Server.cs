namespace WindowsPhoneJsonWireServer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Threading.Tasks;
    using System.Windows;

    using Newtonsoft.Json;

    using Windows.Networking.Connectivity;
    using Windows.Networking.Sockets;
    using Windows.Storage.Streams;

    public class Server
    {
        #region Fields

        private readonly int listeningPort;

        private Automator automator;

        private bool isServerActive;

        private StreamSocketListener listener;

        #endregion

        #region Constructors and Destructors

        public Server(int port)
        {
            this.listeningPort = port;
        }

        #endregion

        #region Delegates

        public delegate void Output(string data);

        #endregion

        #region Public Methods and Operators

        public string FindIpAddress()
        {
            var ipAddresses = new List<string>();
            var hostnames = NetworkInformation.GetHostNames();
            const int IanaInterfaceTypeWiFi = 71; // IanaInterfaceType == 71 => Wifi
            const int IanaInterfaceTypeEthernet = 6; // IanaInterfaceType == 6 => Ethernet (Emulator)
            foreach (var hn in hostnames)
            {
                if (hn.IPInformation != null
                    && (hn.IPInformation.NetworkAdapter.IanaInterfaceType == IanaInterfaceTypeWiFi
                        || hn.IPInformation.NetworkAdapter.IanaInterfaceType == IanaInterfaceTypeEthernet))
                {
                    var ipAddress = hn.DisplayName;
                    ipAddresses.Add(ipAddress);
                }
            }

            if (ipAddresses.Count < 1)
            {
                return null;
            }

            return ipAddresses.Count == 1 ? ipAddresses[0] : ipAddresses[ipAddresses.Count - 1];
        }

        public void SetAutomator(UIElement visualRoot)
        {
            this.automator = new Automator(visualRoot);
        }

        public async void Start()
        {
            if (this.isServerActive)
            {
                return;
            }

            this.isServerActive = true;
            this.listener = new StreamSocketListener();
            this.listener.Control.QualityOfService = SocketQualityOfService.Normal;
            this.listener.ConnectionReceived += this.ListenerConnectionReceived;
            await this.listener.BindServiceNameAsync(this.listeningPort.ToString(CultureInfo.InvariantCulture));
            this.WriteIpAddress();
        }

        public void Stop()
        {
            if (this.isServerActive)
            {
                this.listener.Dispose();
                this.isServerActive = false;
            }
        }

        public void WriteIpAddress()
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var sw = new StreamWriter(isoStore.OpenFile("ip.txt", FileMode.OpenOrCreate, FileAccess.Write)))
                {
                    sw.Write(this.FindIpAddress());
                }
            }
        }

        #endregion

        #region Methods

        private async void HandleRequest(StreamSocket socket)
        {
            // Initialize IO classes
            var reader = new DataReader(socket.InputStream) { InputStreamOptions = InputStreamOptions.Partial };
            var writer = new DataWriter(socket.OutputStream) { UnicodeEncoding = UnicodeEncoding.Utf8 };

            var acceptedRequest = new AcceptedRequest();
            await acceptedRequest.AcceptRequest(reader);

            var response = this.ProcessRequest(acceptedRequest.Request, acceptedRequest.Content);

            // create response
            writer.WriteString(Responder.CreateResponse(response));
            await writer.StoreAsync();

            socket.Dispose();
        }

        private async void ListenerConnectionReceived(
            StreamSocketListener sender, 
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            await Task.Run(() => this.HandleRequest(args.Socket));
        }

        private string ProcessRequest(string request, string content)
        {
            var response = string.Empty;
            var urn = Parser.GetRequestUrn(request);
            var command = Parser.GetUrnLastToken(urn);
            string elementId;
            var urnLength = Parser.GetUrnTokensCount(urn);
            switch (command)
            {
                case "ping":
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, "ping");
                    break;

                case "status":
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, this.FindIpAddress());
                    break;

                case "alert_text":
                    response = this.automator.FirstPopupText();
                    break;

                case "accept_alert":
                    this.automator.ClosePopups();
                    break;

                case "dismiss_alert":
                    this.automator.ClosePopups(false);
                    break;

                case "element":
                    var elementObject = JsonConvert.DeserializeObject<FindElementObject>(content);

                    switch (urnLength)
                    {
                        case 3:

                            // this is an absolute elements command ("/session/:sessionId/element"), search from root
                            response = this.automator.PerformElementCommand(elementObject, null);
                            break;
                        case 5:

                            // this is a relative elements command("/session/:sessionId/element/:id/element"), search from specific element
                            var relativeElementId = Parser.GetElementId(urn);
                            response = this.automator.PerformElementCommand(elementObject, relativeElementId);
                            break;
                    }

                    break;

                case "elements":
                    var elementsObject = JsonConvert.DeserializeObject<FindElementObject>(content);

                    switch (urnLength)
                    {
                        case 3:

                            // this is an absolute elements command ("/session/:sessionId/element"), search from root
                            response = this.automator.PerformElementsCommand(elementsObject, null);
                            break;
                        case 5:

                            // this is a relative elements command("/session/:sessionId/element/:id/element"), search from specific element
                            var relativeElementId = Parser.GetElementId(urn);
                            response = this.automator.PerformElementsCommand(elementsObject, relativeElementId);
                            break;
                    }

                    break;

                case "click":
                    elementId = Parser.GetElementId(urn);
                    response = this.automator.PerformClickCommand(elementId);
                    break;

                case "value":
                    elementId = Parser.GetElementId(urn);
                    response = this.automator.PerformValueCommand(elementId, content);
                    break;

                case "text":
                    elementId = Parser.GetElementId(urn);
                    response = this.automator.PerformTextCommand(elementId);
                    break;

                case "displayed":
                    elementId = Parser.GetElementId(urn);
                    response = this.automator.PerformDisplayedCommand(elementId);
                    break;

                case "location":
                    elementId = Parser.GetElementId(urn);
                    response = this.automator.PerformLocationCommand(elementId);
                    break;

                default:
                    response = "Unimplemented";
                    break;
            }

            return response;
        }

        #endregion
    }
}
