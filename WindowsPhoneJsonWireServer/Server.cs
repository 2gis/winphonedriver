using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.Windows;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace WindowsPhoneJsonWireServer {
    public class Server {

        private readonly int listeningPort;
        private StreamSocketListener listener;
        private bool isServerActive = false;
        private Automator automator;

        public Server(int port) {
            listeningPort = port;
        }

        public void SetAutomator(UIElement visualRoot) {
            automator = new Automator(visualRoot);
        }

        public delegate void Output(String data);

        public async void Start() {
            if (isServerActive) return;
            isServerActive = true;
            listener = new StreamSocketListener();
            listener.Control.QualityOfService = SocketQualityOfService.Normal;
            listener.ConnectionReceived += ListenerConnectionReceived;
            await listener.BindServiceNameAsync(listeningPort.ToString());
            WriteIpAddress();
        }

        public void Stop() {
            if (isServerActive) {
                listener.Dispose();
                isServerActive = false;
            }
        }

        async void ListenerConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args) {
            await Task.Run(() => HandleRequest(args.Socket));
        }


        private async void HandleRequest(StreamSocket socket) {
            //Initialize IO classes
            var reader = new DataReader(socket.InputStream) { InputStreamOptions = InputStreamOptions.Partial };
            var writer = new DataWriter(socket.OutputStream) {
                UnicodeEncoding = UnicodeEncoding.Utf8
            };

            var acceptedRequest = new AcceptedRequest();
            await acceptedRequest.AcceptRequest(reader);

            String response = ProcessRequest(acceptedRequest.request, acceptedRequest.content);

            //create response
            writer.WriteString(Responder.CreateResponse(response));
            await writer.StoreAsync();

            socket.Dispose();
        }

        public void WriteIpAddress() {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            using (var sw = new StreamWriter(isoStore.OpenFile("ip.txt", FileMode.OpenOrCreate, FileAccess.Write))) {
                sw.Write(FindIpAddress());
            }
        }

        private String ProcessRequest(String request, String content) {
            String response = String.Empty;
            String command = Parser.GetRequestCommand(request);
            String elementId = String.Empty;
            int urnLength = Parser.GetUrnTokensLength(request);
            switch (command) {
                case "status":
                    response = Responder.CreateJsonResponse(ResponseStatus.Success, FindIpAddress());
                    break;

                case "element":
                    FindElementObject elementObject = JsonConvert.DeserializeObject<FindElementObject>(content);
                    //this is an absolute elements command ("/session/:sessionId/element"), search from root
                    if (urnLength == 3) {
                        response = automator.PerformElementCommand(elementObject, null);
                    }
                    //this is a relative elements command("/session/:sessionId/element/:id/element"), search from specific element
                    else if (urnLength == 5) {
                        String relativeElementId = Parser.GetElementId(request);
                        response = automator.PerformElementCommand(elementObject, relativeElementId);
                    }
                    break;

                case "elements":
                    FindElementObject elementsObject = JsonConvert.DeserializeObject<FindElementObject>(content);
                    //this is an absolute elements command ("/session/:sessionId/element"), search from root
                    if (urnLength == 3) {
                        response = automator.PerformElementsCommand(elementsObject, null);
                    }
                    //this is a relative elements command("/session/:sessionId/element/:id/element"), search from specific element
                    else if (urnLength == 5) {
                        String relativeElementId = Parser.GetElementId(request);
                        response = automator.PerformElementsCommand(elementsObject, relativeElementId);
                    }
                    break;

                case "click":
                    elementId = Parser.GetElementId(request);
                    response = automator.PerformClickCommand(elementId);
                    break;

                case "value":
                    elementId = Parser.GetElementId(request);
                    response = automator.PerformValueCommand(elementId, content);
                    break;

                case "text":
                    elementId = Parser.GetElementId(request);
                    response = automator.PerformTextCommand(elementId);
                    break;

                case "displayed":
                    elementId = Parser.GetElementId(request);
                    response = automator.PerformDisplayedCommand(elementId);
                    // response = "Unimplemented";
                    break;

                case "location":
                    elementId = Parser.GetElementId(request);
                    response = automator.PerformLocationCommand(elementId);
                    break;

                default:
                    response = "Unimplemented";
                    break;
            }
            return response;
        }

        public String FindIpAddress() {
            List<String> ipAddresses = new List<String>();
            var hostnames = NetworkInformation.GetHostNames();
            foreach (var hn in hostnames) {
                //IanaInterfaceType == 71 => Wifi
                //IanaInterfaceType == 6 => Ethernet (Emulator)
                if (hn.IPInformation != null &&
                    (hn.IPInformation.NetworkAdapter.IanaInterfaceType == 71
                    || hn.IPInformation.NetworkAdapter.IanaInterfaceType == 6)) {
                    String ipAddress = hn.DisplayName;
                    ipAddresses.Add(ipAddress);
                }
            }

            if (ipAddresses.Count < 1)
                return null;
            if (ipAddresses.Count == 1)
                return ipAddresses[0];
            return ipAddresses[ipAddresses.Count - 1];
        }

    }
}
