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

        public String SendRequest(String uri, String requestBody)
        {
            //create the request
            String url = CreateUrl(uri);
            HttpWebRequest request = CreateWebRequest(url, requestBody);

            //send the request and get the response
            var response = (HttpWebResponse)request.GetResponse();

            //read and return the response
            var reader = new StreamReader(response.GetResponseStream());
            string result = reader.ReadToEnd();
            reader.Close();

            return result;
        }

        private String CreateUrl(String uri)
        {
            String fullUrl = "http://" + this.ip + ":" + this.port + uri;
            return fullUrl;
        }

        private HttpWebRequest CreateWebRequest(String url, String body)
        {
            //create request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/json";
            request.Method = GetRequestMethod(url);
            //request.KeepAlive = false;
            
            //write request body
            if (!String.IsNullOrEmpty(body))
            {
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                writer.Write(body);
                writer.Flush();
                writer.Close();
            }

            return request;
        }

        //get the request method depending on the action taking place (the last part of the url) - simple mapping. Improve?
        private String GetRequestMethod(String url)
        {

            return "POST"; //STUB

            //parse url
            String lastToken = RequestParser.GetLastToken(url);

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
