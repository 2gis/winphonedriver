namespace OuterDriver
{
    using System;

    internal class Program
    {
        #region Methods

        [STAThread]
        private static void Main(string[] args)
        {
            var listeningPort = 9999;

            var options = new CommandLineOptions();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                if (options.Port.HasValue)
                {
                    listeningPort = options.Port.Value;
                }
            }

            var listener = new Listener(listeningPort);
            Parser.UrnPrefix = options.UrlBase;

            listener.StartListening();
        }

        #endregion
    }
}
