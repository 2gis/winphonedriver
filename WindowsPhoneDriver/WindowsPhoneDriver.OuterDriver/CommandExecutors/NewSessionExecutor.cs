namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    using Newtonsoft.Json.Linq;

    using OpenQA.Selenium.Remote;

    using WindowsPhoneDriver.Common;
    using WindowsPhoneDriver.OuterDriver.EmulatorHelpers;

    internal class NewSessionExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            this.Automator.ActualCapabilities = ParseDesiredCapabilitiesJson(
                this.ExecutedCommand.ParametersAsJsonString);
            var debugConnectToRunningApp =
                Convert.ToBoolean(this.Automator.ActualCapabilities["debugConnectToRunningApp"]);
            var innerIp = this.InitializeApplication(debugConnectToRunningApp);
            const int InnerPort = 9998;
            Console.WriteLine("Inner ip: " + innerIp);
            this.Automator.CommandForwarder = new Requester(innerIp, InnerPort);

            long timeout = Convert.ToInt32(this.Automator.ActualCapabilities["launchTimeout"]);
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
            Thread.Sleep(Convert.ToInt32(this.Automator.ActualCapabilities["launchDelay"]));

            // gives sometime to load visuals
            var jsonResponse = this.JsonResponse(ResponseStatus.Success, this.Automator.ActualCapabilities);

            return jsonResponse;
        }

        private static Dictionary<string, object> ParseDesiredCapabilitiesJson(string content)
        {
            /* Parses JSON and returns dictionary of supported capabilities and their values (or default values if not set)
             * launchTimeout - App launch timeout (app is pinged every 0.5 sec within launchTimeout;
             * launchDelay - gives time for visuals to initialize after app launch (successful ping)
             * reaching timeout will not raise any error, it will still wait for launchDelay and try to execute next command
            */
            var supportedCapabilities = new Dictionary<string, object>
                                            {
                                                { "app", string.Empty }, 
                                                { "platform", "WinPhone" }, 
                                                { "deviceName", string.Empty }, 
                                                { "launchDelay", 0 }, 
                                                { "launchTimeout", 10000 }, 
                                                { "debugConnectToRunningApp", "false" }, 
                                            };

            var actualCapabilities = new Dictionary<string, object>();
            var parsedContent = JObject.Parse(content);
            var desiredCapabilitiesToken = parsedContent["desiredCapabilities"];
            if (desiredCapabilitiesToken != null)
            {
                var desiredCapabilities = desiredCapabilitiesToken.ToObject<Dictionary<string, object>>();
                foreach (var capability in supportedCapabilities)
                {
                    object value;
                    if (!desiredCapabilities.TryGetValue(capability.Key, out value))
                    {
                        value = capability.Value;
                    }

                    actualCapabilities.Add(capability.Key, value);
                }
            }
            else
            {
                actualCapabilities = supportedCapabilities;
            }

            return actualCapabilities;
        }

        private string InitializeApplication(bool debugDoNotDeploy = false)
        {
            var appPath = this.Automator.ActualCapabilities["app"].ToString();
            this.Automator.Deployer = new Deployer81(this.Automator.ActualCapabilities["deviceName"].ToString());
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
