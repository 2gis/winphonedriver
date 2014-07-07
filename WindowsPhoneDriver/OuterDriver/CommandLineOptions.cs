namespace OuterDriver
{
    using CommandLine;
    using CommandLine.Text;

    internal class CommandLineOptions
    {
        #region Public Properties

        [Option("port", Required = false, HelpText = "port to listen on")]
        public int? Port { get; set; }

        [Option("url-base", Required = false, HelpText = "base URL path prefix for commands, e.g. wd/url")]
        public string UrlBase { get; set; }

        #endregion

        #region Public Methods and Operators

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        #endregion
    }
}
