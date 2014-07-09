namespace WindowsPhoneDriver.OuterDriver.EmulatorHelpers.NativeMethods
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;

    internal partial class NativeMethods
    {
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(
            IntPtr hdcDest, 
            int xDest, 
            int yDest, 
            int wDest, 
            int hDest, 
            IntPtr hdcSource, 
            int xSrc, 
            int ySrc, 
            CopyPixelOperation rop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hDc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);
    }
}
