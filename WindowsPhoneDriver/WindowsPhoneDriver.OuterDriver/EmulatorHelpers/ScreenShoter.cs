namespace WindowsPhoneDriver.OuterDriver.EmulatorHelpers
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Windows.Forms;

    internal class ScreenShoter
    {
        #region Public Methods and Operators

        public static string TakeScreenshot()
        {
            var bmp = ScreenToBitmap();
            var base64 = ToBase64String(bmp, ImageFormat.Png);
            bmp.Dispose();
            return base64;
        }

        #endregion

        // P/Invoke declarations generated code
        #region Methods

        private static Bitmap ScreenToBitmap()
        {
            var sz = Screen.PrimaryScreen.Bounds.Size;
            var desktopWindow = NativeMethods.NativeMethods.GetDesktopWindow();
            var windowDc = NativeMethods.NativeMethods.GetWindowDC(desktopWindow);
            var destination = NativeMethods.NativeMethods.CreateCompatibleDC(windowDc);
            var bitmap = NativeMethods.NativeMethods.CreateCompatibleBitmap(windowDc, sz.Width, sz.Height);
            var oldBitmap = NativeMethods.NativeMethods.SelectObject(destination, bitmap);
            NativeMethods.NativeMethods.BitBlt(
                destination, 
                0, 
                0, 
                sz.Width, 
                sz.Height, 
                windowDc, 
                0, 
                0, 
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
            var bmp = Image.FromHbitmap(bitmap);
            NativeMethods.NativeMethods.SelectObject(destination, oldBitmap);
            NativeMethods.NativeMethods.DeleteObject(bitmap);
            NativeMethods.NativeMethods.DeleteDC(destination);
            NativeMethods.NativeMethods.ReleaseDC(desktopWindow, windowDc);
            return bmp;
        }

        private static string ToBase64String(Bitmap bmp, ImageFormat imageFormat)
        {
            var memoryStream = new MemoryStream();
            bmp.Save(memoryStream, imageFormat);

            memoryStream.Position = 0;
            var byteBuffer = memoryStream.ToArray();

            memoryStream.Close();

            var base64String = Convert.ToBase64String(byteBuffer);

            return base64String;
        }

        #endregion
    }
}
