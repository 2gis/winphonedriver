namespace WindowsPhoneDriver.OuterDriver
{
    #region using

    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    using global::WindowsPhoneDriver.OuterDriver;

    #endregion

    internal class OuterServer
    {
        #region Fields

        private readonly Requester requester;

        #endregion

        #region Constructors and Destructors

        public OuterServer(string innerIp, int innerPort)
        {
            this.requester = new Requester(innerIp, innerPort);
        }

        #endregion

        #region Public Methods and Operators

        public static string FindIpAddress()
        {
            var localIp = "localhost";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            {
                localIp = ip.ToString();
            }

            return localIp;
        }

        public string SendRequest(string uri, string requestBody)
        {
            return this.requester.SendRequest(uri, requestBody);
        }

        public string SendRequest(string uri)
        {
            return this.requester.SendRequest(uri, string.Empty);
        }

        #endregion
    }
}
