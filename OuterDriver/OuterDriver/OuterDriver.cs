using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OuterDriver
{
    class OuterDriver
    {

        public static void ClickEmulatorScreenPoint(Point point) {
            const String emulatorProcessName = "XDE.exe";
            int xOffset = point.X;
            int yOffset = point.Y;
            const int SW_RESTORE = 9;
            Process[] procs = Process.GetProcesses();
            if (procs.Length != 0) {
                for (int i = 0; i < procs.Length; i++) {
                    try {
                        if (procs[i].MainModule.ModuleName == emulatorProcessName) {
                            IntPtr hwnd = procs[i].MainWindowHandle;
                            SetForegroundWindow(hwnd);
                            ShowWindow(hwnd, SW_RESTORE);
                            ClickOnPoint(hwnd, new Point(xOffset, yOffset));
                            return;
                        }
                    }
                    catch (Exception ex) {
                        //Console.WriteLine(ex.GetType() + ex.Message);
                    }
                }
            }
            else {
                Console.WriteLine("No emulator running");
                return;
            }
        }

        public static void ClickEnter()
        {
            //ugly hardcoded stuff. I'm sorry
            int xOffset = 315;
            int yOffset = 545;

            ClickEmulatorScreenPoint(new Point(xOffset, yOffset));
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        private static void ClickOnPoint(IntPtr wndHandle, Point clientPoint)
        {
            var oldPos = Cursor.Position;

            // get screen coordinates
            ClientToScreen(wndHandle, ref clientPoint);

            // set cursor on coords, and press mouse
            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);
            Thread.Sleep(1000);
            mouse_event(0x00000002, 0, 0, 0, UIntPtr.Zero); // left mouse button down
            mouse_event(0x00000004, 0, 0, 0, UIntPtr.Zero); // left mouse button up

            // return mouse 
            Cursor.Position = oldPos;
        }

        [DllImport("user32.dll")]
        private static extern bool
        SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(
        IntPtr hWnd, int nCmdShow);

        private static void SwitchToEmulator()
        {
            const int SW_RESTORE = 9;
            Process[] procs = Process.GetProcesses();
            if (procs.Length != 0)
            {
                for (int i = 0; i < procs.Length; i++)
                {
                    try
                    {
                        if (procs[i].MainModule.ModuleName ==
                           "XDE.exe")
                        {
                            IntPtr hwnd = procs[i].MainWindowHandle;
                            SetForegroundWindow(hwnd);
                            ShowWindow(hwnd, SW_RESTORE);
                            ClickOnPoint(hwnd, new Point(10, 10));

                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.GetType() + ex.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("No process running");
                return;
            }
        }
    
    
    }
}
