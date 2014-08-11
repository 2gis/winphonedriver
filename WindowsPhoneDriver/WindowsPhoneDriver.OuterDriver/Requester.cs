namespace WindowsPhoneDriver.OuterDriver
{
    using System;
    using System.IO;
    using System.Net;

    using Newtonsoft.Json;

    using OpenQA.Selenium.Remote;

    internal class Requester
    {
        #region Fields

        private readonly string ip;

        private readonly int port;

        #endregion

        #region Constructors and Destructors

        public Requester(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        #endregion

        #region Public Methods and Operators

        public string ForwardCommand(Command commandToForward, bool verbose = true)
        {
            var serializedCommand = JsonConvert.SerializeObject(commandToForward);

            return this.SendRequest(serializedCommand, verbose);
        }

        public string SendRequest(string requestContent, bool verbose)
        {
            var result = "UnknownError";
            StreamReader reader = null;
            WebResponse response = null;
            try
            {
                // create the request
                var uri = string.Format("http://{0}:{1}", this.ip, this.port);
                var request = CreateWebRequest(uri, requestContent);
                if (verbose)
                {
                    // TODO Write normal logging
                    Console.WriteLine("Sending request: " + requestContent + " to " + uri);
                }

                // send the request and get the response
                response = request.GetResponse();
                var stream = response.GetResponseStream();
                if (stream == null)
                {
                    throw new NullReferenceException();
                }

                // read and return the response
                reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                if (verbose)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }
            }

            return result;
        }

        #endregion

        #region Methods

        private static HttpWebRequest CreateWebRequest(string uri, string content)
        {
            // create request
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.KeepAlive = false;

            // write request body
            if (!string.IsNullOrEmpty(content))
            {
                var writer = new StreamWriter(request.GetRequestStream());
                writer.Write(content);
                writer.Close();
            }

            return request;
        }

        #endregion
    }
}
