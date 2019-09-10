using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Atomex.Client.Wpf.Common
{
    public static class BitmapExtensions
    {
        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                rect: new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                flags: ImageLockMode.ReadOnly,
                format: bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                pixelWidth: bitmapData.Width,
                pixelHeight: bitmapData.Height,
                dpiX: bitmap.HorizontalResolution,
                dpiY: bitmap.VerticalResolution,
                pixelFormat: PixelFormats.Bgr32,
                palette: null,
                buffer: bitmapData.Scan0,
                bufferSize: bitmapData.Stride * bitmapData.Height,
                stride: bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }
    }
}