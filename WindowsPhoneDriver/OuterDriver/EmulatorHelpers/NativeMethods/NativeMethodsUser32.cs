namespace OuterDriver.EmulatorHelpers.NativeMethods
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    internal partial class NativeMethods
    {
        // An application-defined callback function used with the EnumWindows & EnumChildWindows http://msdn.microsoft.com/en-us/library/windows/desktop/ms633498(v=vs.85).aspx
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // The mouse_event function synthesizes mouse motion and button clicks http://msdn.microsoft.com/en-us/library/windows/desktop/ms646260(v=vs.85).aspx
        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        // Activate window to receive keyboard input http://msdn.microsoft.com/en-us/library/windows/desktop/ms633539(v=vs.85).aspx
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // Retrieves the identifier of the thread that created the specified window http://msdn.microsoft.com/en-us/library/windows/desktop/ms633522(v=vs.85).aspx
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // Enumerates all top-level windows on the screen with callback http://msdn.microsoft.com/en-us/library/windows/desktop/ms633497(v=vs.85).aspx
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);

        // Enumerates the child windows that belong to the specified parent window with callback http://msdn.microsoft.com/en-us/library/windows/desktop/ms633494(v=vs.85).aspx
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        // Copies the text of the specified window into a buffer http://msdn.microsoft.com/en-us/library/windows/desktop/ms633520(v=vs.85).aspx
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // Retrieves the length, in characters, of the specified window's text http://msdn.microsoft.com/en-us/library/windows/desktop/ms633521(v=vs.85).aspx
        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        // Determines the visibility state of the specified window http://msdn.microsoft.com/en-us/library/windows/desktop/ms633530(v=vs.85).aspx
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        // Retrieves a handle to the Shell's desktop window http://msdn.microsoft.com/en-us/library/windows/desktop/ms633512(v=vs.85).aspx
        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        // Retrieves the dimensions of the bounding rectangle of the specified window http://msdn.microsoft.com/en-us/library/windows/desktop/ms633519(v=vs.85).aspx
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        // Brings the specified window to the top of the Z order. http://msdn.microsoft.com/en-us/library/windows/desktop/ms632673(v=vs.85).aspx
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr ptr);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;

            public int Top;

            public int Right;

            public int Bottom;

            public int Width()
            {
                return this.Right - this.Left;
            }

            public int Height()
            {
                return this.Bottom - this.Top;
            }
        }
    }
}
