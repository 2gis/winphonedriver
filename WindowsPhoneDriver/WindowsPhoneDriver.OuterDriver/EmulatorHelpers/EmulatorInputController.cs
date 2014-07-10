namespace WindowsPhoneDriver.OuterDriver.EmulatorHelpers
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;

    using WindowsPhoneDriver.Common;

    internal class EmulatorInputController
    {
        #region Fields

        private readonly IntPtr outputWindowHandle; // Used to determine WP screen size (480x800 or 480x853)

        private readonly IntPtr xdeWindowHandle; // Used to obtain host window coordinates and size

        private int mouseMovementSleepDelay;

        #endregion

        #region Constructors and Destructors

        public EmulatorInputController(string emulatorName)
        {
            this.mouseMovementSleepDelay = 0;
            var procs = Process.GetProcessesByName("XDE");
            foreach (var proc in procs)
            {
                var wnds = NativeWrapper.GetOpenWindowsFromPid(proc.Id);

                bool isXdeOfInterest = wnds.Any(x => x.Key.StartsWith(emulatorName));

                // Using StartWith instead of Equals because emulator name for 8.1 ends with locale, e.g. (RU)
                if (isXdeOfInterest)
                {
                    wnds.TryGetValue("XDE", out this.xdeWindowHandle);

                    // Host XDE window, which allows determining Emulator screen size in terms of host screen
                    this.outputWindowHandle =
                        NativeWrapper.GetChildWindowsFromHwnd(this.xdeWindowHandle)
                            .FirstOrDefault(x => x.Key.Equals("Output Painter Window"))
                            
                            // Output window, which allows determining Phone screen size
                            .Value;
                    break;
                }
            }
        }

        #endregion

        #region Enums

        public enum PhoneOrientation
        {
            Portrait, 

            Landscape
        }

        #endregion

        #region Public Properties

        public int MouseMovementSmoothing
        {
            // Mouse movement delay that can be used in tests debugging
            get
            {
                return this.mouseMovementSleepDelay;
            }

            set
            {
                this.mouseMovementSleepDelay = value > 0 ? value : 0;
            }
        }

        #endregion

        #region Public Methods and Operators

        public void ClickEnterKey()
        {
            // Replaced with relative coordinates, still hard-coded, need more thought
            var phoneScreenSize = this.PhoneScreenSize();

            var offsetX = phoneScreenSize.Width - 30;
            var offsetY = phoneScreenSize.Height - 40;

            this.LeftClickPhoneScreenAtPoint(new Point(offsetX, offsetY));
        }

        public PhoneOrientation EstimatePhoneOrientation()
        {
            var screen = this.PhoneScreenSize();
            return screen.Width > screen.Height ? PhoneOrientation.Landscape : PhoneOrientation.Portrait;
        }

        public void LeftButtonDown()
        {
            NativeWrapper.SendMouseButtonEvent(NativeWrapper.ButtonFlags.LeftDown);
        }

        public void LeftButtonUp()
        {
            NativeWrapper.SendMouseButtonEvent(NativeWrapper.ButtonFlags.LeftUp);
        }

        public void LeftClick()
        {
            this.LeftButtonDown();
            this.LeftButtonUp();
        }

        public void LeftClickPhoneScreenAtPoint(Point point)
        {
            var oldPos = Cursor.Position;

            this.MoveCursorToPhoneScreenAtPoint(point);
            this.LeftButtonDown();
            this.LeftButtonUp();

            // return mouse back to original position
            Cursor.Position = oldPos;
        }

        public void MoveCursorToPhoneScreenAtPoint(Point phonePoint)
        {
            this.SwitchToEmulator();

            var hostPoint = this.TranslatePhonePointToHostPoint(phonePoint);
            if (!this.PhonePointVisibleOnScreen(phonePoint))
            {
                throw new AutomationException(
                    string.Format(
                        "Location {0}:{1} is out of phone screen bounds {2}:{3}. Scroll into view before clicking.", 
                        phonePoint.X, 
                        phonePoint.Y, 
                        this.PhoneScreenSize().Width, 
                        this.PhoneScreenSize().Height), 
                    ResponseStatus.MoveTargetOutOfBounds);
            }

            this.LinearSmoothMoveCursorToHostAtPoint(new Point(hostPoint.X, hostPoint.Y));
        }

        public bool PhonePointVisibleOnScreen(Point phonePoint)
        {
            var phoneScreen = new Rectangle(new Point(0, 0), this.PhoneScreenSize());
            return phoneScreen.Contains(phonePoint);
        }

        public Size PhoneScreenSize()
        {
            return NativeWrapper.GetWindowRectangle(this.outputWindowHandle).Size;
        }

        #endregion

        #region Methods

        private Rectangle HostRectangle()
        {
            return NativeWrapper.GetWindowRectangle(this.xdeWindowHandle);
        }

        private void LinearSmoothMoveCursorToHostAtPoint(Point newPosition)
        {
            var start = Cursor.Position;
            PointF iterPoint = start;
            const int Steps = 10;

            // Find the slope of the line segment defined by start and newPosition
            var slope = new PointF(newPosition.X - start.X, newPosition.Y - start.Y);

            // Divide by the number of steps
            slope.X = slope.X / Steps;
            slope.Y = slope.Y / Steps;

            // Move the mouse to each iterative point.
            for (int i = 0; i < Steps; i++)
            {
                iterPoint = new PointF(iterPoint.X + slope.X, iterPoint.Y + slope.Y);
                Cursor.Position = Point.Round(iterPoint);
                Thread.Sleep(this.MouseMovementSmoothing);
            }

            // Move the mouse to the final destination.
            Cursor.Position = newPosition;
        }

        private void SwitchToEmulator()
        {
            NativeWrapper.BringWindowToForegroundAndActivate(this.xdeWindowHandle);
        }

        private Point TranslatePhonePointToHostPoint(Point phonePoint)
        {
            var hostScreen = this.HostRectangle();
            var phoneScreenSize = this.PhoneScreenSize();

            var translatedPoint = new Point();

            var scaleFactorX = (double)hostScreen.Width / phoneScreenSize.Width;
            var scaleFactorY = (double)hostScreen.Height / phoneScreenSize.Height;

            translatedPoint.X = (int)(phonePoint.X * scaleFactorX) + hostScreen.X;
            translatedPoint.Y = (int)(phonePoint.Y * scaleFactorY) + hostScreen.Y;

            return translatedPoint;
        }

        #endregion
    }
}
