namespace WindowsPhoneDriver.OuterDriver.Automator
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal class Capabilities
    {
        #region Constructors and Destructors

        private Capabilities()
        {
            this.App = string.Empty;
            this.DeviceName = string.Empty;
            this.LaunchDelay = 0;
            this.LaunchTimeout = 10000;
            this.DebugConnectToRunningApp = false;
        }

        #endregion

        #region Public Properties

        [JsonProperty("app")]
        public string App { get; set; }

        [JsonProperty("debugConnectToRunningApp")]
        public bool DebugConnectToRunningApp { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("launchDelay")]
        public int LaunchDelay { get; set; }

        [JsonProperty("launchTimeout")]
        public int LaunchTimeout { get; set; }

        [JsonProperty("platformName")]
        public string PlatformName
        {
            get
            {
                return "WindowsPhone";
            }
        }

        #endregion

        #region Public Methods and Operators

        public static Capabilities CapabilitiesFromJsonString(string jsonString)
        {
            var capabilities = JsonConvert.DeserializeObject<Capabilities>(
                jsonString, 
                new JsonSerializerSettings
                    {
                        Error =
                            delegate(object sender, ErrorEventArgs args)
                                {
                                    args.ErrorContext.Handled = true;
                                }
                    });

            return capabilities;
        }

        public string CapabilitiesToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }

        #endregion
    }
}
