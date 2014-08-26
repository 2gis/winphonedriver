namespace WindowsPhoneDriver.OuterDriver
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;

    using OpenQA.Selenium.Remote;

    using WindowsPhoneDriver.Common;

    public class Listener
    {
        #region Static Fields

        private static string urnPrefix;

        #endregion

        #region Fields

        private UriDispatchTables dispatcher;

        private CommandExecutorDispatchTable executorDispatcher;

        private TcpListener listener;

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

        public void StartListening()
        {
            try
            {
                this.listener = new TcpListener(IPAddress.Any, this.Port);

                this.Prefix = new Uri(string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}", this.Port));
                this.dispatcher = new UriDispatchTables(new Uri(this.Prefix, UrnPrefix));
                this.executorDispatcher = new CommandExecutorDispatchTable();

                // Start listening for client requests.
                this.listener.Start();

                // Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests. 
                    var client = this.listener.AcceptTcpClient();

                    // Get a stream object for reading and writing
                    using (var stream = client.GetStream())
                    {
                        var acceptedRequest = new AcceptedRequest();
                        acceptedRequest.AcceptRequest(stream);

                        var responseBody = this.HandleRequest(acceptedRequest);

                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write(responseBody);
                            writer.Flush();
                        }

                        // Shutdown and end connection
                    }

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

        private string HandleRequest(AcceptedRequest acceptedRequest)
        {
            var request = acceptedRequest.Request;
            var content = acceptedRequest.Content;

            var firstHeaderTokens = request.Split(' ');
            var method = firstHeaderTokens[0];
            var resourcePath = firstHeaderTokens[1];

            var uriToMatch = new Uri(this.Prefix, resourcePath);
            var matched = this.dispatcher.Match(method, uriToMatch);

            if (matched == null)
            {
                return HttpResponseHelper.ResponseString(HttpStatusCode.NotFound, "Unknown command " + uriToMatch);
            }

            var commandName = matched.Data.ToString();
            var commandToExecute = new Command(commandName, content);
            foreach (string variableName in matched.BoundVariables.Keys)
            {
                commandToExecute.Parameters[variableName] = matched.BoundVariables[variableName];
            }

            return this.ProcessCommand(commandToExecute);
        }

        private string ProcessCommand(Command command)
        {
            var executor = this.executorDispatcher.GetExecutor(command.Name);
            executor.ExecutedCommand = command;
            var respnose = executor.Do();
            return respnose;
        }

        #endregion
    }
}
