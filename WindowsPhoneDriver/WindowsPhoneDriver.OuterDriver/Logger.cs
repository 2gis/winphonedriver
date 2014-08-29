namespace WindowsPhoneDriver.OuterDriver
{
    using System.ComponentModel;

    using NLog;
    using NLog.Targets;

    internal class Logger
    {
        #region Static Fields

        private static readonly NLog.Logger Log;

        #endregion

        #region Constructors and Destructors

        static Logger()
        {
            Log = LogManager.GetLogger("outerdriver");

            var target = new ColoredConsoleTarget { Layout = "${date:format=HH\\:MM\\:ss} [${level:uppercase=true}] ${message}" };

            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Debug);
            LogManager.ReconfigExistingLoggers();
        }

        #endregion

        #region Enums

        public enum LogVerbositiy
        {
            Verbose, 

            Silent
        }

        #endregion

        #region Public Methods and Operators

        public static void Debug([Localizable(false)] string message, params object[] args)
        {
            Log.Debug(message, args);
        }

        public static void Error([Localizable(false)] string message, params object[] args)
        {
            Log.Error(message, args);
        }

        public static void Fatal([Localizable(false)] string message, params object[] args)
        {
            Log.Fatal(message, args);
        }

        public static void Info([Localizable(false)] string message, params object[] args)
        {
            Log.Info(message, args);
        }

        public static void Trace([Localizable(false)] string message, params object[] args)
        {
            Log.Trace(message, args);
        }

        public static void Warn([Localizable(false)] string message, params object[] args)
        {
            Log.Warn(message, args);
        }

        #endregion
    }
}
