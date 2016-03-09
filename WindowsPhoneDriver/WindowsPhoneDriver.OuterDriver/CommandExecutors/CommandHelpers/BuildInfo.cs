namespace WindowsPhoneDriver.OuterDriver.CommandExecutors.CommandHelpers
{
    #region

    using System.Reflection;

    using Newtonsoft.Json;

    #endregion

    public class BuildInfo
    {
        #region Static Fields

        private static string version;

        #endregion

        #region Public Properties

        [JsonProperty("version")]
        public string Version
        {
            get
            {
                return version ?? (version = Assembly.GetExecutingAssembly().GetName().Version.ToString());
            }
        }

        #endregion

        #region Public Methods and Operators

        public override string ToString()
        {
            return string.Format("Version: {0}", this.Version);
        }

        #endregion
    }
}