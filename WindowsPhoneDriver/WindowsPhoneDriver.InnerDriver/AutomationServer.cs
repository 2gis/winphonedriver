namespace WindowsPhoneDriver.InnerDriver
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    using Windows.Networking.Connectivity;
    using Windows.Networking.Sockets;
    using Windows.Storage.Streams;

    using WindowsPhoneDriver.Common;

    public class AutomationServer
    {
        #region Static Fields

        public static readonly AutomationServer Instance = new AutomationServer();

        #endregion

        #region Fields

        private Automator automator;

        private bool isServerActive;

        private StreamSocketListener listener;

        private int listeningPort;

        #endregion

        #region Delegates

        public delegate void Output(string data);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Initializes and starts <see cref="AutomationServer"/> with specified parameters.
        /// </summary>
        /// <remarks>Use it in conjuction with <see cref="Instance"/> to simplify inclusion of server in tested app.</remarks>
        /// <param name="port"></param>
        /// <param name="visualRoot"></param>
        public void InitializeAndStart(UIElement visualRoot, int port = 9998)
        {
            this.SetAutomator(visualRoot);
            this.Start(port);
        }

        public void SetAutomator(UIElement visualRoot)
        {
            this.automator = new Automator(visualRoot);
        }

        public async void Start(int port)
        {
            if (this.isServerActive)
            {
                return;
            }

            this.listeningPort = port;

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

        #endregion

        #region Methods

        internal string FindIpAddress()
        {
            var hostnames = NetworkInformation.GetHostNames();
            const int IanaInterfaceTypeWiFi = 71; // IanaInterfaceType == 71 => WiFi
            const int IanaInterfaceTypeEthernet = 6; // IanaInterfaceType == 6 => Ethernet (Emulator)
            var ipAddresses = (from hn in hostnames
                               where
                                   hn.IPInformation != null
                                   && (hn.IPInformation.NetworkAdapter.IanaInterfaceType == IanaInterfaceTypeWiFi
                                       || hn.IPInformation.NetworkAdapter.IanaInterfaceType == IanaInterfaceTypeEthernet)
                               select hn.DisplayName).ToList();

            if (ipAddresses.Count < 1)
            {
                return null;
            }

            return ipAddresses.Count == 1 ? ipAddresses[0] : ipAddresses[ipAddresses.Count - 1];
        }

        internal void WriteIpAddress()
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var sw = new StreamWriter(isoStore.OpenFile("ip.txt", FileMode.OpenOrCreate, FileAccess.Write)))
                {
                    sw.Write(this.FindIpAddress());
                }
            }
        }

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
            var urn = RequestParser.GetRequestUrn(request);

            return this.automator.ProcessCommand(urn, content);
        }

        #endregion
    }
}
