using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace OuterDriver.EmulatorHelpers
{
    internal class NativeHelpers
    {
        #region Native WinApi Wrappers

        public static Rectangle GetWindowRectangle(IntPtr hWnd)
        {
            Rect winRect;
            if (GetWindowRect(hWnd, out winRect))
            {
                return new Rectangle(winRect.Left, winRect.Top, winRect.Width(), winRect.Height());
            }
            return Rectangle.Empty;
        }

        public static IDictionary<string, IntPtr> GetOpenWindowsFromPid(int processId)
        {
            var hShellWindow = GetShellWindow();
            var dictWindows = new Dictionary<string, IntPtr>();

            EnumWindows(delegate(IntPtr hWnd, int lParam)
            {
                if (hWnd == hShellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                var length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                uint windowPid;
                GetWindowThreadProcessId(hWnd, out windowPid);
                if (windowPid != processId) return true;

                var stringBuilder = new StringBuilder(length);
                GetWindowText(hWnd, stringBuilder, length + 1);
                dictWindows.Add(stringBuilder.ToString(), hWnd);
                return true;
            }, 0);

            return dictWindows;
        }

        public static IDictionary<string, IntPtr> GetChildWindowsFromHwnd(IntPtr hwnd)
        {
            var dictWindows = new Dictionary<string, IntPtr>();

            EnumChildWindows(hwnd, delegate(IntPtr hWnd, int lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;

                var length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                var stringBuilder = new StringBuilder(length);
                GetWindowText(hWnd, stringBuilder, length + 1);
                dictWindows.Add(stringBuilder.ToString(), hWnd);
                return true;
            }, 0);

            return dictWindows;
        }

        public static bool BringWindowToForegroundAndActivate(IntPtr hWnd)
        {
            var bringToFrontResult = BringWindowToTop(hWnd);
            var setForegroundResult = SetForegroundWindow(hWnd);

            return bringToFrontResult && setForegroundResult;
        }

        public enum ButtonFlags : uint
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
        }

        public static void SendMouseButtonEvent(ButtonFlags flags)
        {
            mouse_event((uint) flags, 0, 0, 0, UIntPtr.Zero);
        }

        #endregion

        #region Native WinAPI methods

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width()
            {
                return Right - Left;
            }

            public int Height()
            {
                return Bottom - Top;
            }
        }

        // The mouse_event function synthesizes mouse motion and button clicks http://msdn.microsoft.com/en-us/library/windows/desktop/ms646260(v=vs.85).aspx
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        // Activate window to receive keyboard input http://msdn.microsoft.com/en-us/library/windows/desktop/ms633539(v=vs.85).aspx
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Retrieves the identifier of the thread that created the specified window http://msdn.microsoft.com/en-us/library/windows/desktop/ms633522(v=vs.85).aspx
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // An application-defined callback function used with the EnumWindows & EnumChildWindows http://msdn.microsoft.com/en-us/library/windows/desktop/ms633498(v=vs.85).aspx
        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        // Enumerates all top-level windows on the screen with callback http://msdn.microsoft.com/en-us/library/windows/desktop/ms633497(v=vs.85).aspx
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        // Enumerates the child windows that belong to the specified parent window with callback http://msdn.microsoft.com/en-us/library/windows/desktop/ms633494(v=vs.85).aspx
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, int lParam);

        // Copies the text of the specified window into a buffer http://msdn.microsoft.com/en-us/library/windows/desktop/ms633520(v=vs.85).aspx
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // Retrieves the length, in characters, of the specified window's text http://msdn.microsoft.com/en-us/library/windows/desktop/ms633521(v=vs.85).aspx
        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        // Determines the visibility state of the specified window http://msdn.microsoft.com/en-us/library/windows/desktop/ms633530(v=vs.85).aspx
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // Retrieves a handle to the Shell's desktop window http://msdn.microsoft.com/en-us/library/windows/desktop/ms633512(v=vs.85).aspx
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        // Retrieves the dimensions of the bounding rectangle of the specified window http://msdn.microsoft.com/en-us/library/windows/desktop/ms633519(v=vs.85).aspx
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        // Brings the specified window to the top of the Z order. http://msdn.microsoft.com/en-us/library/windows/desktop/ms632673(v=vs.85).aspx
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        #endregion
    }
}