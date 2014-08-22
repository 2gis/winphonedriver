namespace WindowsPhoneDriver.OuterDriver.CommandExecutors
{
    using System;
    using System.Drawing;

    using WindowsPhoneDriver.OuterDriver.Automator;
    using WindowsPhoneDriver.OuterDriver.EmulatorHelpers;

    internal class TouchFlickExecutor : CommandExecutorBase
    {
        #region Methods

        protected override string DoImpl()
        {
            this.Automator.UpdatedOrientationForEmulatorController();

            var screen = this.Automator.EmulatorController.PhoneScreenSize;
            var startPoint = new Point(screen.Width / 2, screen.Height / 2);
            var elementId = Automator.GetValue<string>(this.ExecutedCommand.Parameters, "element");
            if (elementId != null)
            {
                startPoint = this.Automator.RequestElementLocation(elementId).GetValueOrDefault();
            }

            object speed;
            if (this.ExecutedCommand.Parameters.TryGetValue("speed", out speed))
            {
                var xOffset = Convert.ToInt32(this.ExecutedCommand.Parameters["xoffset"]);
                var yOffset = Convert.ToInt32(this.ExecutedCommand.Parameters["yoffset"]);

                this.Automator.EmulatorController.PerformGesture(
                    new FlickGesture(startPoint, xOffset, yOffset, Convert.ToDouble(speed)));
            }
            else
            {
                var xSpeed = Convert.ToDouble(this.ExecutedCommand.Parameters["xspeed"]);
                var ySpeed = Convert.ToDouble(this.ExecutedCommand.Parameters["yspeed"]);
                this.Automator.EmulatorController.PerformGesture(new FlickGesture(startPoint, xSpeed, ySpeed));
            }

            return null;
        }

        #endregion
    }
}
