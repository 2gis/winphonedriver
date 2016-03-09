namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    #region using

    using System;
    using System.Diagnostics;
    using System.Threading;

    using Newtonsoft.Json;

    using OpenQA.Selenium.Remote;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.OuterDriver.Automator;

    #endregion

    internal class NewSessionExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            // It is easier to reparse desired capabilities as JSON instead of re-mapping keys to attributes and calling type conversions, 
            // so we will take possible one time performance hit by serializing Dictionary and deserializing it as Capabilities object
            var serializedCapability =
                JsonConvert.SerializeObject(this.ExecutedCommand.Parameters["desiredCapabilities"]);
            this.Automator.ActualCapabilities = Capabilities.CapabilitiesFromJsonString(serializedCapability);

            var innerIp = this.InitializeApplication(this.Automator.ActualCapabilities.DebugConnectToRunningApp);

            this.Automator.CommandForwarder = new Requester(innerIp, this.Automator.ActualCapabilities.InnerPort);

            this.ConnectToApp();

            // TODO throw AutomationException with SessionNotCreatedException if timeout and uninstall the app

            // Gives sometime to load visuals (needed only in case of slow emulation)
            Thread.Sleep(this.Automator.ActualCapabilities.LaunchDelay);

            var jsonResponse = this.JsonResponse(ResponseStatus.Success, this.Automator.ActualCapabilities);

            return jsonResponse;
        }

        private void ConnectToApp()
        {
            long timeout = this.Automator.ActualCapabilities.LaunchTimeout;

            const int PingStep = 500;
            var stopWatch = new Stopwatch();
            while (timeout > 0)
            {
                stopWatch.Restart();

                Logger.Trace("Ping inner driver");
                var pingCommand = new Command(null, "ping", null);
                var responseBody = this.Automator.CommandForwarder.ForwardCommand(pingCommand, false, 2000);
                if (responseBody.StartsWith("<pong>", StringComparison.Ordinal))
                {
                    break;
                }

                Thread.Sleep(PingStep);
                stopWatch.Stop();
                timeout -= stopWatch.ElapsedMilliseconds;
            }
        }

        private string InitializeApplication(bool debugDoNotDeploy = false)
        {
            var appPath = this.Automator.ActualCapabilities.App;
            this.Automator.Deployer = new Winium.Mobile.Connectivity.Deployer(this.Automator.ActualCapabilities.DeviceName, false);
            if (!debugDoNotDeploy)
            {
                this.Automator.Deployer.Install(appPath, null);
                this.Automator.Deployer.Launch();
            }

            this.Automator.ActualCapabilities.DeviceName = this.Automator.Deployer.DeviceName;
            var emulatorController = this.Automator.CreateEmulatorController(debugDoNotDeploy);
            this.Automator.EmulatorController = emulatorController;

            return this.Automator.EmulatorController.GetIpAddress();
        }

        #endregion
    }
}
