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
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        private static void ClickOnPoint(IntPtr wndHandle, Point clientPoint)
        {
            var oldPos = Cursor.Position;

            /// get screen coordinates
            ClientToScreen(wndHandle, ref clientPoint);

            /// set cursor on coords, and press mouse
            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);
            Thread.Sleep(1000);
            mouse_event(0x00000002, 0, 0, 0, UIntPtr.Zero); /// left mouse button down
            mouse_event(0x00000004, 0, 0, 0, UIntPtr.Zero); /// left mouse button up

            /// return mouse 
            Cursor.Position = oldPos;
        }

        [DllImport("user32.dll")]
        private static extern bool
        SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(
        IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "PostMessage")]
        private static extern bool PostMessage(IntPtr hWnd, UInt32 msg, int wParam, int lParam);

        private enum KeyEvent
        {
            KeyUp = 0x0002,
            KeyDown = 0x0000,
            ExtendedKey = 0x0001
        }

        //private struct KEYBDINPUT
        //{
        //    public ushort wVk;
        //    public ushort wScan;
        //    public uint dwFlags;
        //    public long time;
        //    public uint dwExtraInfo;
        //};

        //[StructLayout(LayoutKind.Explicit, Size = 28)]
        //private struct INPUT
        //{
        //    [FieldOffset(0)]
        //    public uint type;
        //    [FieldOffset(4)]
        //    public KEYBDINPUT ki;
        //};

        //[DllImport("user32.dll", SetLastError = true)]
        //private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

        //[StructLayout(LayoutKind.Sequential)]
        //internal struct INPUT
        //{
        //    public uint Type;
        //    public MOUSEKEYBDHARDWAREINPUT Data;
        //}

        ///// <summary>
        ///// http://social.msdn.microsoft.com/Forums/en/csharplanguage/thread/f0e82d6e-4999-4d22-b3d3-32b25f61fb2a
        ///// </summary>
        //[StructLayout(LayoutKind.Explicit)]
        //internal struct MOUSEKEYBDHARDWAREINPUT
        //{
        //    [FieldOffset(0)]
        //    public KEYBDINPUT Keyboard;
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //internal struct KEYBDINPUT
        //{
        //    public ushort Vk;
        //    public ushort Scan;
        //    public uint Flags;
        //    public uint Time;
        //    public IntPtr ExtraInfo;
        //}

        ///// <summary>
        ///// simulate key press
        ///// </summary>
        ///// <param name="keyCode"></param>
        //public static void SendKeyPress(ushort keyCode)
        //{
        //    INPUT input = new INPUT
        //    {
        //        Type = 1
        //    };
        //    input.Data.Keyboard = new KEYBDINPUT()
        //    {
        //        Vk = (ushort)keyCode,
        //        Scan = 0,
        //        Flags = 0,
        //        Time = 0,
        //        ExtraInfo = IntPtr.Zero,
        //    };

        //    INPUT input2 = new INPUT
        //    {
        //        Type = 1
        //    };
        //    input2.Data.Keyboard = new KEYBDINPUT()
        //    {
        //        Vk = (ushort)keyCode,
        //        Scan = 0,
        //        Flags = 2,
        //        Time = 0,
        //        ExtraInfo = IntPtr.Zero
        //    };
        //    INPUT[] inputs = new INPUT[] { input, input2 };
        //    if (SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
        //        throw new Exception();
        //}

        ///// <summary>
        ///// Send a key down and hold it down until sendkeyup method is called
        ///// </summary>
        ///// <param name="keyCode"></param>
        //public static void SendKeyDown(ushort keyCode)
        //{
        //    INPUT input = new INPUT
        //    {
        //        Type = 1
        //    };
        //    input.Data.Keyboard = new KEYBDINPUT();
        //    input.Data.Keyboard.Vk = (ushort)keyCode;
        //    input.Data.Keyboard.Scan = 0;
        //    input.Data.Keyboard.Flags = 0;
        //    input.Data.Keyboard.Time = 0;
        //    input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
        //    INPUT[] inputs = new INPUT[] { input };
        //    if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
        //    {
        //        throw new Exception();
        //    }
        //}

        ///// <summary>
        ///// Release a key that is being hold down
        ///// </summary>
        ///// <param name="keyCode"></param>
        //public static void SendKeyUp(ushort keyCode)
        //{
        //    INPUT input = new INPUT
        //    {
        //        Type = 1
        //    };
        //    input.Data.Keyboard = new KEYBDINPUT();
        //    input.Data.Keyboard.Vk = (ushort)keyCode;
        //    input.Data.Keyboard.Scan = 0;
        //    input.Data.Keyboard.Flags = 2;
        //    input.Data.Keyboard.Time = 0;
        //    input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
        //    INPUT[] inputs = new INPUT[] { input };
        //    if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
        //        throw new Exception();

        //}

        public static void ClickEnter()
        {
            int xOffset = 315;
            int yOffset = 545;
            const int SW_RESTORE = 9;
            Process[] procs = Process.GetProcesses();
            if (procs.Length != 0)
            {
                for (int i = 0; i < procs.Length; i++)
                {
                    try
                    {
                        Console.WriteLine(procs[i].MainModule.ModuleName);
                        if (procs[i].MainModule.ModuleName ==
                           "XDE.exe")
                        {
                            IntPtr hwnd = procs[i].MainWindowHandle;
                            SetForegroundWindow(hwnd);
                            ShowWindow(hwnd, SW_RESTORE);
                            ClickOnPoint(hwnd, new Point(xOffset, yOffset));

                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.GetType() + ex.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("No process running");
                return;
            }
        }

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
                        Console.WriteLine(procs[i].MainModule.ModuleName);
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
                        Console.WriteLine(ex.GetType() + ex.Message);
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
