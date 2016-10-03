namespace WindowsPhoneDriver.Common
{
    public class ConnectionInformation
    {
        #region Public Properties

        public const string FileName = ".WindowsPhoneDriver.ConnectionInformation.json";

        public string RemotePort { get; set; }

        public override string ToString()
        {
            return string.Format("RemotePort: {0}", this.RemotePort);
        }

        #endregion
    }
}