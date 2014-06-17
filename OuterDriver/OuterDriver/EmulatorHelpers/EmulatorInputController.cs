using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace OuterDriver.EmulatorHelpers
{
    internal class EmulatorInputController
    {
        private readonly IntPtr _xdeHandle; // Used to obtain host window coordinates and size
        private readonly IntPtr _wpHandle; // Used to determine WP screen size (480x800 or 480x853)
        private int _mouseMovementSleepDelay;

        public int MouseMovmentSmoothing // Mouse movement delay that can be used in tests debugging
        {
            get { return _mouseMovementSleepDelay; }
            set { _mouseMovementSleepDelay = value > 0 ? value : 0; }
        }

        public EmulatorInputController(string emulatorName)
        {
            _mouseMovementSleepDelay = 0;
            var procs = Process.GetProcessesByName("XDE");
            foreach (var proc in procs)
            {
                var wnds = NativeHelpers.GetOpenWindowsFromPid(proc.Id);

                bool isXdeOfInterest = wnds.Any(x => x.Key.StartsWith(emulatorName));
                // Using StartWith instead of Equals because emulator name for 8.1 ends with locale, e.g. (RU)
                if (isXdeOfInterest)
                {
                    wnds.TryGetValue("XDE", out _xdeHandle);
                        // Host XDE window, which allows determining Emulator screen size in terms of host screen
                    _wpHandle =
                        NativeHelpers.GetChildWindowsFromHwnd(_xdeHandle)
                            .FirstOrDefault(x => x.Key.Equals("Output Painter Window"))
                            // Output window, which allows determining Phone screen size
                            .Value;
                    break;
                }
            }
        }

        public void MoveCursorToPhoneScreenAtPoint(Point clientPoint)
        {
            SwitchToEmulator();
            var hostPoint = TranslatePhonePointToHostPoint(clientPoint);
            LinearSmoothMoveCursorToHostAtPoint(new Point(hostPoint.X, hostPoint.Y));
        }

        public void LeftClickPhoneScreenAtPoint(Point point)
        {
            var oldPos = Cursor.Position;

            MoveCursorToPhoneScreenAtPoint(point);
            LeftButtonDown();
            LeftButtonUp();

            // return mouse back to original position
            Cursor.Position = oldPos;
        }

        public void LeftClick()
        {
            LeftButtonDown();
            LeftButtonUp();
        }

        public void LeftButtonDown()
        {
            NativeHelpers.SendMouseButtonEvent(NativeHelpers.ButtonFlags.LeftDown);
        }

        public void LeftButtonUp()
        {
            NativeHelpers.SendMouseButtonEvent(NativeHelpers.ButtonFlags.LeftUp);
        }

        public void ClickEnterKey()
        {
            // Replaced with relative coordinates, still hard-coded, need more thought
            var phoneScreenSize = PhoneScreenSize();

            var xOffset = phoneScreenSize.Width - 30;
            var yOffset = phoneScreenSize.Height - 40;

            LeftClickPhoneScreenAtPoint(new Point(xOffset, yOffset));
        }

        public enum PhoneOrientation
        {
            Portrait,
            Landscape
        };

        public PhoneOrientation EstimatePhoneOrientation()
        {
            var screen = PhoneScreenSize();
            return screen.Width > screen.Height ? PhoneOrientation.Landscape : PhoneOrientation.Portrait;
        }

        private Point TranslatePhonePointToHostPoint(Point phonePoint)
        {
            var hostScreen = HostRectangle();
            var phoneScreenSize = PhoneScreenSize();

            var translatedPoint = new Point();

            var xScaleFactor = (double) hostScreen.Width/phoneScreenSize.Width;
            var yScaleFactor = (double) hostScreen.Height/phoneScreenSize.Height;

            translatedPoint.X = (int) (phonePoint.X*xScaleFactor) + hostScreen.X;
            translatedPoint.Y = (int) (phonePoint.Y*yScaleFactor) + hostScreen.Y;

            return translatedPoint;
        }

        private Size PhoneScreenSize()
        {
            return NativeHelpers.GetWindowRectangle(_wpHandle).Size;
        }

        private Rectangle HostRectangle()
        {
            return NativeHelpers.GetWindowRectangle(_xdeHandle);
        }

        private void LinearSmoothMoveCursorToHostAtPoint(Point newPosition)
        {
            var start = Cursor.Position;
            PointF iterPoint = start;
            const int steps = 10;


            // Find the slope of the line segment defined by start and newPosition
            var slope = new PointF(newPosition.X - start.X, newPosition.Y - start.Y);

            // Divide by the number of steps
            slope.X = slope.X/steps;
            slope.Y = slope.Y/steps;

            // Move the mouse to each iterative point.
            for (int i = 0; i < steps; i++)
            {
                iterPoint = new PointF(iterPoint.X + slope.X, iterPoint.Y + slope.Y);
                Cursor.Position = Point.Round(iterPoint);
                Thread.Sleep(MouseMovmentSmoothing);
            }

            // Move the mouse to the final destination.
            Cursor.Position = newPosition;
        }

        private void SwitchToEmulator()
        {
            NativeHelpers.BringWindowToForegroundAndActivate(_xdeHandle);
        }
    }
}