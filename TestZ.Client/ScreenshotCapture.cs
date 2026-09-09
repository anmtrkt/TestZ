using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TestZ.Client;

/// <summary>
/// Taking screenshot from all monitors
/// </summary>
public static class ScreenshotCapture


{
    public static byte[] CaptureJpeg(int quality = Settings.JpegQuality)
    {
        int x = WinApi.GetSystemMetrics(WinApi.SM_XVIRTUALSCREEN);
        int y = WinApi.GetSystemMetrics(WinApi.SM_YVIRTUALSCREEN);
        int width = WinApi.GetSystemMetrics(WinApi.SM_CXVIRTUALSCREEN);
        int height = WinApi.GetSystemMetrics(WinApi.SM_CYVIRTUALSCREEN);

        IntPtr screenDc = WinApi.GetDC(IntPtr.Zero);
        try
        {
            IntPtr memDc = WinApi.CreateCompatibleDC(screenDc);
            try
            {
                IntPtr bmp = WinApi.CreateCompatibleBitmap(screenDc, width, height);
                try
                {
                    // image from th screen to bitmap
                    IntPtr old = WinApi.SelectObject(memDc, bmp);
                    WinApi.BitBlt(memDc, 0, 0, width, height, screenDc, x, y, WinApi.SrcCopy);
                    WinApi.SelectObject(memDc, old);
                    
                    var bmi = new WinApi.BITMAPINFO();
                    bmi.BmiHeader.BiSize = (uint)Marshal.SizeOf<WinApi.BITMAPINFOHEADER>();
                    bmi.BmiHeader.BiWidth = width;
                    bmi.BmiHeader.BiHeight = -height; 
                    bmi.BmiHeader.BiPlanes = 1;
                    bmi.BmiHeader.BiBitCount = 32;
                    bmi.BmiHeader.BiCompression = 0;  
                    var pixels = new byte[width * height * 4];
                    _ = WinApi.GetDIBits(memDc, bmp, 0, (uint)height, pixels, ref bmi, WinApi.DibRgbColors);

                    // converting to bytes
                    var source = BitmapSource.Create(width, height, 96, 96,
                        PixelFormats.Bgr32, null, pixels, width * 4);
                    var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                    encoder.Frames.Add(BitmapFrame.Create(source));
                    using var ms = new MemoryStream();
                    encoder.Save(ms);
                    return ms.ToArray();
                }
                finally { _ = WinApi.DeleteObject(bmp); }
            }
            finally { _ = WinApi.DeleteDC(memDc); }
        }
        finally { _ = WinApi.ReleaseDC(IntPtr.Zero, screenDc); }
        
    }
}