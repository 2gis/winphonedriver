namespace OuterDriver
{
    using System;
    using System.IO;
    using System.Net;

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

        public string SendRequest(string urn, string requestContent)
        {
            string result = "error";
            StreamReader reader = null;
            WebResponse response = null;
            try
            {
                // create the request
                string uri = this.CreateUri(urn);
                HttpWebRequest request = this.CreateWebRequest(uri, requestContent);
                Console.WriteLine("Sending request: " + requestContent + " to " + uri);

                // send the request and get the response
                response = request.GetResponse();

                // read and return the response
                reader = new StreamReader(response.GetResponseStream());
                result = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        private string CreateUri(string urn)
        {
            var uri = "http://" + this.ip + ":" + this.port + urn;
            return uri;
        }

        private HttpWebRequest CreateWebRequest(string uri, string content)
        {
            // create request
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.ContentType = "application/json";
            request.Method = RequestParserEx.ChooseRequestMethod(uri);
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
