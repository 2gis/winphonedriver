namespace OuterDriver.EmulatorHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Text;

    internal class NativeWrapper
    {
        #region Native WinApi Wrappers

        public enum ButtonFlags : uint
        {
            LeftDown = 0x00000002, 

            LeftUp = 0x00000004, 
        }

        public static Rectangle GetWindowRectangle(IntPtr windowHandle)
        {
            NativeMethods.NativeMethods.Rect winRect;
            return NativeMethods.NativeMethods.GetWindowRect(windowHandle, out winRect)
                       ? new Rectangle(winRect.Left, winRect.Top, winRect.Width(), winRect.Height())
                       : Rectangle.Empty;
        }

        public static IDictionary<string, IntPtr> GetOpenWindowsFromPid(int processId)
        {
            var shellWindow = NativeMethods.NativeMethods.GetShellWindow();
            var dictWindows = new Dictionary<string, IntPtr>();

            NativeMethods.NativeMethods.EnumWindows(
                delegate(IntPtr windowHandle, IntPtr leftParam)
                    {
                        if (windowHandle == shellWindow)
                        {
                            return true;
                        }

                        if (!NativeMethods.NativeMethods.IsWindowVisible(windowHandle))
                        {
                            return true;
                        }

                        var length = NativeMethods.NativeMethods.GetWindowTextLength(windowHandle);
                        if (length == 0)
                        {
                            return true;
                        }

                        uint windowPid;
                        NativeMethods.NativeMethods.GetWindowThreadProcessId(windowHandle, out windowPid);
                        if (windowPid != processId)
                        {
                            return true;
                        }

                        var stringBuilder = new StringBuilder(length);
                        NativeMethods.NativeMethods.GetWindowText(windowHandle, stringBuilder, length + 1);
                        dictWindows.Add(stringBuilder.ToString(), windowHandle);
                        return true;
                    }, 
                IntPtr.Zero);

            return dictWindows;
        }

        public static IDictionary<string, IntPtr> GetChildWindowsFromHwnd(IntPtr hwnd)
        {
            var dictWindows = new Dictionary<string, IntPtr>();

            NativeMethods.NativeMethods.EnumChildWindows(
                hwnd, 
                delegate(IntPtr windowHandle, IntPtr leftParam)
                    {
                        if (!NativeMethods.NativeMethods.IsWindowVisible(windowHandle))
                        {
                            return true;
                        }

                        var length = NativeMethods.NativeMethods.GetWindowTextLength(windowHandle);
                        if (length == 0)
                        {
                            return true;
                        }

                        var stringBuilder = new StringBuilder(length);
                        NativeMethods.NativeMethods.GetWindowText(windowHandle, stringBuilder, length + 1);
                        dictWindows.Add(stringBuilder.ToString(), windowHandle);
                        return true;
                    }, 
                IntPtr.Zero);

            return dictWindows;
        }

        public static bool BringWindowToForegroundAndActivate(IntPtr windowHandle)
        {
            var bringToFrontResult = NativeMethods.NativeMethods.BringWindowToTop(windowHandle);
            var setForegroundResult = NativeMethods.NativeMethods.SetForegroundWindow(windowHandle);

            return bringToFrontResult && setForegroundResult;
        }

        public static void SendMouseButtonEvent(ButtonFlags flags)
        {
            NativeMethods.NativeMethods.mouse_event((uint)flags, 0, 0, 0, UIntPtr.Zero);
        }

        #endregion
    }
}
