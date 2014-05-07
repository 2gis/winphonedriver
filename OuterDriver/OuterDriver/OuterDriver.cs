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

namespace OuterDriver {
    class OuterDriver {

        private static int borderXOffset = 35;
        private static int borderYOffset = 72;
        private static int statusBarSize = 20;

        public static void MoveCursorToEmulatorCoordinates(Point clientPoint) {
            const String emulatorProcessName = "XDE.exe";
            const int SW_RESTORE = 9;
            Process[] procs = Process.GetProcesses();
            if (procs.Length != 0) {
                for (int i = 0; i < procs.Length; i++) {
                    try {
                        if (procs[i].MainModule.ModuleName == emulatorProcessName) {
                            IntPtr hwnd = procs[i].MainWindowHandle;
                            SetForegroundWindow(hwnd);
                            ShowWindow(hwnd, SW_RESTORE);
                            ClientToScreen(hwnd, ref clientPoint);
                            Cursor.Position = new Point(clientPoint.X + borderXOffset, clientPoint.Y + borderYOffset);
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

        public static void ClickEmulatorScreenPoint(Point point) {
            var oldPos = Cursor.Position;

            MoveCursorToEmulatorCoordinates(point);
            mouse_event(0x00000002, 0, 0, 0, UIntPtr.Zero); // left mouse button down
            mouse_event(0x00000004, 0, 0, 0, UIntPtr.Zero); // left mouse button up

            // return mouse 
            Cursor.Position = oldPos;
        }

        public static void ClickEnter() {
            //ugly hardcoded stuff. I'm truly sorry
            int xOffset = 445;
            int yOffset = 760;

            ClickEmulatorScreenPoint(new Point(xOffset, yOffset));
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool
        SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(
        IntPtr hWnd, int nCmdShow);

        private static void SwitchToEmulator() {
            const int SW_RESTORE = 9;
            Process[] procs = Process.GetProcesses();
            if (procs.Length != 0) {
                for (int i = 0; i < procs.Length; i++) {
                    try {
                        if (procs[i].MainModule.ModuleName == "XDE.exe") {
                            IntPtr hwnd = procs[i].MainWindowHandle;
                            SetForegroundWindow(hwnd);
                            ShowWindow(hwnd, SW_RESTORE);
                            ClickEmulatorScreenPoint(new Point(-10, -10));

                            return;
                        }
                    }
                    catch (Exception ex) {
                        //Console.WriteLine(ex.GetType() + ex.Message);
                    }
                }
            }
            else {
                Console.WriteLine("No process running");
                return;
            }
        }


    }
}
