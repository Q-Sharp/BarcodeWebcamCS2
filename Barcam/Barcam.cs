using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AForge.Video;
using ZXing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Barcam
{
    public class Barcammer : IDisposable
    {
        private IEnumerable<FilterInfo> Devices = new FilterInfoCollection(FilterCategory.VideoInputDevice).Cast<FilterInfo>();
        private FilterInfo oSelectedDevice;
        private VideoCaptureDevice CapureDevice;
        private volatile string sResult;

        private IBarcodeReader oReader = new BarcodeReader(null, x => new BitmapSourceLuminanceSource(Convert(x)), null);

        public class BarcammerEventArgs : EventArgs
        {
            public BarcammerEventArgs(string sResult) => Result = sResult;
            public string Result { get; set; }
        }

        public class BitmapEventArgs : EventArgs
        {
            public BitmapEventArgs(Bitmap oBitmap) => Bitmap = oBitmap;
            public Bitmap Bitmap { get; set; }
        }

        protected Barcammer()
        {
        }

        public static Barcammer Create() => new Barcammer();

        public string SelectedDevice => oSelectedDevice.Name;
        public IEnumerable<string> DeviceNames => Devices.Select(x => x.Name).ToArray();
        public string Result => sResult;

        public Barcammer SetDevice(string sName = null)
        {
            var oDevice = Devices.FirstOrDefault(x => x.Name == sName) ?? Devices.FirstOrDefault();

            if (oDevice != null && oDevice.Name != oSelectedDevice?.Name)
            {
                Dispose();
                oSelectedDevice = oDevice;
                CapureDevice = new VideoCaptureDevice(oSelectedDevice.MonikerString);
            }

            return this;
        }

        public Barcammer StartWebcamBarcodeCapture()
        {
            if (CapureDevice != null)
            {
                CapureDevice.VideoResolution = CapureDevice.VideoCapabilities.LastOrDefault();
                CapureDevice.NewFrame += CapureDevice_NewFrame;
                CapureDevice.Start();
            }

            return this;
        }

        private async void CapureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            var oBitmap = eventArgs.Frame.Clone();
            var oBitmap2 = eventArgs.Frame.Clone();

            OnBitmapChanged(new BitmapEventArgs((Bitmap)oBitmap));
            await TryDecode((Bitmap)oBitmap2);
        }

        private async Task TryDecode(Bitmap oFrame)
        {
            sResult = await Task.Run(() =>
            {
                try
                {
                    oReader.Options.PossibleFormats = new BarcodeFormat[]
                        {
                            BarcodeFormat.QR_CODE,
                            BarcodeFormat.All_1D,
                            BarcodeFormat.DATA_MATRIX
                        };

                    var oResults = oReader.DecodeMultiple(oFrame);
                    return oResults.Select(x => x.ToString()).Aggregate((b1, b2) => $@"{b1}, {b2}");
                }
                catch (Exception)
                {
                    return null;
                }
            });

            if (!string.IsNullOrEmpty(sResult))
                OnResultFound(new BarcammerEventArgs(sResult));
        }

        public void Dispose()
        {
            if (CapureDevice?.IsRunning == true)
                CapureDevice.Stop();
        }

        private static BitmapSource Convert(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        public event EventHandler<BarcammerEventArgs> ResultFound;
        public event EventHandler<BitmapEventArgs> BitmapChanged;

        protected virtual void OnResultFound(BarcammerEventArgs e) => ResultFound?.Invoke(this, e);
        protected virtual void OnBitmapChanged(BitmapEventArgs e) => BitmapChanged?.Invoke(this, e);
    }
}
