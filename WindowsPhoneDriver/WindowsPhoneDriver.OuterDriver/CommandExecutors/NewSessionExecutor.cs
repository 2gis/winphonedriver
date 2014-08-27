namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using Newtonsoft.Json;

    using OpenQA.Selenium.Remote;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.OuterDriver.Automator;
    using WindowsPhoneDriver.OuterDriver.EmulatorHelpers;

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
            const int InnerPort = 9998;
            Console.WriteLine("Inner ip: " + innerIp);
            this.Automator.CommandForwarder = new Requester(innerIp, InnerPort);

            long timeout = this.Automator.ActualCapabilities.LaunchTimeout;

            const int PingStep = 500;
            var stopWatch = new Stopwatch();
            while (timeout > 0)
            {
                stopWatch.Restart();
                Console.Write(".");
                var pingCommand = new Command(null, "ping", null);
                var responseBody = this.Automator.CommandForwarder.ForwardCommand(pingCommand, false, 2000);
                if (responseBody.StartsWith("<pong>"))
                {
                    break;
                }

                Thread.Sleep(PingStep);
                stopWatch.Stop();
                timeout -= stopWatch.ElapsedMilliseconds;
            }

            Console.WriteLine();

            // Gives sometime to load visuals (needed only in case of slow emulation)
            Thread.Sleep(this.Automator.ActualCapabilities.LaunchDelay);

            var jsonResponse = this.JsonResponse(ResponseStatus.Success, this.Automator.ActualCapabilities);

            return jsonResponse;
        }

        private string InitializeApplication(bool debugDoNotDeploy = false)
        {
            var appPath = this.Automator.ActualCapabilities.App;
            this.Automator.Deployer = new Deployer81(this.Automator.ActualCapabilities.DeviceName);
            if (!debugDoNotDeploy)
            {
                this.Automator.Deployer.Deploy(appPath);
            }

            Console.WriteLine("Actual Device: " + this.Automator.Deployer.DeviceName);
            var emulatorController = new EmulatorController(this.Automator.Deployer.DeviceName);
            this.Automator.EmulatorController = emulatorController;

            return this.Automator.EmulatorController.GetIpAddress();
        }

        #endregion
    }
}
