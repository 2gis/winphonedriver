namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using Newtonsoft.Json;

    using OpenQA.Selenium.Remote;

    using WindowsPhoneDriver.Common;

    using DriverCommand = OpenQA.Selenium.Remote.DriverCommand;

    internal class MouseMoveToExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            object elementId;
            this.ExecutedCommand.Parameters.TryGetValue("element", out elementId);

            var coordinates = new Point();
            if (elementId != null)
            {
                var parameters = new Dictionary<string, object> { { "ID", elementId } };
                var locationCommand = new Command(null, DriverCommand.GetElementLocation, parameters);

                var responseBody = this.Automator.CommandForwarder.ForwardCommand(locationCommand);

                var deserializeObject = JsonConvert.DeserializeObject<JsonResponse>(responseBody);
                if (deserializeObject.Status == ResponseStatus.Success)
                {
                    var values =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(deserializeObject.Value.ToString());
                    coordinates.X = Convert.ToInt32(values["x"]);
                    coordinates.Y = Convert.ToInt32(values["y"]);
                }
            }
            else
            {
                var xOffset = this.ExecutedCommand.Parameters["xoffset"].ToString();
                var yOffset = this.ExecutedCommand.Parameters["yoffset"].ToString();
                coordinates = new Point(int.Parse(xOffset), int.Parse(yOffset));
            }

            this.Automator.UpdatedOrientationForEmulatorController();
            this.Automator.EmulatorController.MoveCursorTo(coordinates);

            return null;
        }

        #endregion
    }
}
