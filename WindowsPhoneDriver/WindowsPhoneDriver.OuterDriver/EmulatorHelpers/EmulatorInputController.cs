namespace WindowsPhoneDriver.OuterDriver.EmulatorHelpers
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;

    using Microsoft.Xde.Common;
    using Microsoft.Xde.Wmi;

    internal class EmulatorInputController
    {
        #region Fields

        private readonly IXdeVirtualMachine emulatorVm;

        private readonly IntPtr outputWindowHandle; // Used to determine WP screen size (480x800 or 480x853)

        private Point cursor;

        #endregion

        #region Constructors and Destructors

        public EmulatorInputController(string emulatorName)
        {
            this.emulatorVm = GetEmulatorVm(emulatorName);
            this.cursor = new Point(0, 0);

            var partialName = emulatorName.Split('(')[0];

            if (this.emulatorVm == null)
            {
                throw new NullReferenceException(
                    string.Format("Could not get running XDE virtual machine by partial name {0}", partialName));
            }

            var procs = Process.GetProcessesByName("XDE");
            foreach (var process in procs)
            {
                var windows = NativeWrapper.GetOpenWindowsFromPid(process.Id);

                var isXdeOfInterest = windows.Any(x => x.Key.StartsWith(partialName));

                // Using StartWith instead of Equals because emulator name for 8.1 ends with locale, e.g. (RU)
                if (isXdeOfInterest)
                {
                    // Host XDE window, which allows determining Emulator screen size in terms of host screen
                    IntPtr xdeWindowHandle;
                    windows.TryGetValue("XDE", out xdeWindowHandle);

                    // Output window, which allows determining Phone screen size
                    this.outputWindowHandle =
                        NativeWrapper.GetChildWindowsFromHwnd(xdeWindowHandle)
                            .FirstOrDefault(x => x.Key.Equals("Output Painter Window"))
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

        #region Public Methods and Operators

        public PhoneOrientation EstimatePhoneOrientation()
        {
            var screen = this.PhoneScreenSize();
            return screen.Width > screen.Height ? PhoneOrientation.Landscape : PhoneOrientation.Portrait;
        }

        public void LeftButtonDown()
        {
            var hold = new MouseEventArgs(MouseButtons.Left, 0, this.cursor.X, this.cursor.Y, 0);
            this.emulatorVm.SendMouseEvent(hold);
        }

        public void LeftButtonUp()
        {
            var release = new MouseEventArgs(MouseButtons.None, 0, this.cursor.X, this.cursor.Y, 0);
            this.emulatorVm.SendMouseEvent(release);
        }

        public void LeftClick()
        {
            this.LeftButtonDown();
            this.LeftButtonUp();
        }

        public void LeftClickPhoneScreenAtPoint(Point point)
        {
            var hold = new MouseEventArgs(MouseButtons.Left, 0, point.X, point.Y, 0);
            var release = new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0);
            this.emulatorVm.SendMouseEvent(hold);
            this.emulatorVm.SendMouseEvent(release);
        }

        public void MoveCursorTo(Point phonePoint)
        {
            this.cursor = phonePoint;
        }

        public void PerformGesture(IGesture gesture)
        {
            // TODO Works only for default portrait orientation, need to take orientation into account
            var array = gesture.GetScreenPoints().ToArray();

            foreach (var point in array.Take(array.Length - 1))
            {
                this.MoveCursorTo(point);
                this.LeftButtonDown();
                Thread.Sleep(gesture.PeriodBetweenPoints);
            }

            this.MoveCursorTo(array.Last());
            this.LeftButtonUp();
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

        public void PressEnterKey()
        {
            this.emulatorVm.TypeKey(Keys.Enter);
        }

        #endregion

        #region Methods

        private static IXdeVirtualMachine GetEmulatorVm(string emulatorName)
        {
            var factory = new XdeWmiFactory();
            var vm = factory.GetVirtualMachine(emulatorName + "." + Environment.UserName);
            if (vm.EnabledState != VirtualMachineEnabledState.Enabled)
            {
                throw new XdeVirtualMachineException("Emulator is not running. ");
            }

            return vm;
        }

        #endregion
    }
}
