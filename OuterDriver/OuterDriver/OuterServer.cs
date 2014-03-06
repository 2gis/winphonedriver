using System;
using System.Linq;
using System.Net;

namespace OuterDriver
{
    class OuterServer
    {

        private Requester requester;

        public OuterServer(String innerIp, int innerPort)
        {
            this.requester = new Requester(innerIp, innerPort);
        }

        public String SendRequest(String uri, String requestBody)
        {
            return requester.SendRequest(uri, requestBody);
        }

        public String SendRequest(String uri)
        {
            return requester.SendRequest(uri, String.Empty);
        }

        public static String FindIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

    }
}
