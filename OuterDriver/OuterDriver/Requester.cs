using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace OuterDriver
{
    class Requester
    {

        private readonly String ip;
        private readonly String port;

        public Requester(String ip, String port)
        {
            this.ip = ip;
            this.port = port;
        }

        public String SendRequest(String urn, String requestBody)
        {
            String result = "error";
            StreamReader reader = null;
            WebResponse response = null;
            try
            {
                //create the request
                String uri = CreateUri(urn);
                HttpWebRequest request = CreateWebRequest(uri, requestBody);
                //send the request and get the response
                response = (HttpWebResponse)request.GetResponse();

                Console.WriteLine("Sending " + request + " to " + uri);

                //read and return the response
                reader = new StreamReader(response.GetResponseStream());
                result = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                response.Close();
                reader.Close();
            }
            return result;
        }

        private String CreateUri(String urn)
        {
            String uri = "http://" + this.ip + ":" + this.port + urn;
            return uri;
        }

        private HttpWebRequest CreateWebRequest(String uri, String body)
        {
            //create request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.ContentType = "application/json";
            request.Method = GetRequestMethod(uri);
            request.KeepAlive = false;
            
            //write request body
            if (!String.IsNullOrEmpty(body))
            {
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                writer.Write(body);
                writer.Close();
            }

            return request;
        }

        //get the request method depending on the action taking place (the last part of the url) - simple mapping. Improve?
        private String GetRequestMethod(String uri)
        {

            return "POST"; //STUB

            //parse url
            String lastToken = RequestParser.GetLastToken(uri);

            //get value from pre-filled Dictionary
            switch (lastToken)
            {
                case "element":
                    return "POST";
                case "click":
                    return "GET";
                default:
                    return "Unexpected command";
            }

            
        }
    }
}
