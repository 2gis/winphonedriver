using System;
using System.IO;
using System.Linq;
using System.Net;


namespace OuterDriver {
    class Requester {

        private readonly String ip;
        private readonly int port;

        public Requester(String ip, int port) {
            this.ip = ip;
            this.port = port;
        }

        public String SendRequest(String urn, String requestContent) {
            String result = "error";
            StreamReader reader = null;
            WebResponse response = null;
            try {
                //create the request
                String uri = CreateUri(urn);
                HttpWebRequest request = CreateWebRequest(uri, requestContent);
                Console.WriteLine("Sending request: " + requestContent + " to " + uri);
                //send the request and get the response
                response = request.GetResponse();

                //read and return the response
                reader = new StreamReader(response.GetResponseStream());
                result = reader.ReadToEnd();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            finally {
                if (response != null)
                    response.Close();
                if (reader != null)
                    reader.Close();
            }
            return result;
        }

        private String CreateUri(String urn) {
            String uri = "http://" + this.ip + ":" + this.port + urn;
            return uri;
        }

        private HttpWebRequest CreateWebRequest(String uri, String content) {
            //create request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.ContentType = "application/json";
            request.Method = Parser.ChooseRequestMethod(uri);
            request.KeepAlive = false;

            //write request body
            if (!String.IsNullOrEmpty(content)) {
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                writer.Write(content);
                writer.Close();
            }

            return request;
        }

    }
}
