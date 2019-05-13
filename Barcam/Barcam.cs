using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using ZXing;

namespace Barcam
{
	public class Barcammer : IDisposable
	{
		public class BarcammerEventArgs : EventArgs
		{
			public BarcammerEventArgs(string[] sResults, Rectangle[] oResultRects)
			{
				Results = sResults;
				ResultRects = oResultRects;
			}

			public Rectangle[] ResultRects { get; set; }
			public string[] Results { get; set; }
		}

		public class BitmapEventArgs : EventArgs
		{
			public BitmapEventArgs(Bitmap oBitmap) => Bitmap = oBitmap;
			public Bitmap Bitmap { get; set; }
		}

		private VideoCaptureDevice oCapureDevice;
		private IEnumerable<FilterInfo> oDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice).Cast<FilterInfo>();

		private IBarcodeReader oReader = new BarcodeReader(null, x => new BitmapSourceLuminanceSource(Convert(x)), null);
		private FilterInfo oSelectedDevice;

		public event EventHandler<BitmapEventArgs> BitmapChanged;
		public event EventHandler<BarcammerEventArgs> ResultFound;

		protected Barcammer()
		{
		}

		public IEnumerable<string> DeviceNames => oDevices.Select(x => x.Name).ToArray();
		public string SelectedDevice => oSelectedDevice.Name;
		public static Barcammer Create() => new Barcammer();

		public void Dispose()
		{
			if(oCapureDevice?.IsRunning == true)
				oCapureDevice.Stop();
		}

		public Barcammer SetDevice(string sName = null)
		{
			var oDevice = oDevices.FirstOrDefault(x => x.Name == sName) ?? oDevices.FirstOrDefault();
			if(oDevice != null && oDevice.Name != oSelectedDevice?.Name)
			{
				Dispose();
				oSelectedDevice = oDevice;
				oCapureDevice = new VideoCaptureDevice(oSelectedDevice.MonikerString);
			}
			return this;
		}

		public Barcammer StartWebcamBarcodeCapture()
		{
			try
			{
				if(oCapureDevice != null)
				{
					oCapureDevice.VideoResolution = oCapureDevice.VideoCapabilities
						.GroupBy(x => x,
								 x => x,
								(x, y) => new { MaxBitCount = y.Max(z => z.BitCount), VideoCapabilities = x })
						.FirstOrDefault(x => x.VideoCapabilities.BitCount == x.MaxBitCount)
						.VideoCapabilities;
					oCapureDevice.NewFrame += CapureDevice_NewFrame;
					oCapureDevice.Start();
				}
			}
			catch(Exception)
			{
				oCapureDevice.VideoResolution = oCapureDevice.VideoCapabilities.FirstOrDefault();
			}

			return this;
		}

		protected virtual void OnBitmapChanged(BitmapEventArgs e) => BitmapChanged?.Invoke(this, e);
		protected virtual void OnResultFound(BarcammerEventArgs e) => ResultFound?.Invoke(this, e);

		private async void CapureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
		{
			OnBitmapChanged(new BitmapEventArgs((Bitmap)eventArgs.Frame.Clone()));
			await Task.Run(() => TryDecode((Bitmap)eventArgs.Frame.Clone()));
		}

		static private BitmapSource Convert(Bitmap bitmap)
		{
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
			var bitmapSource = BitmapSource.Create(bitmapData.Width,bitmapData.Height, bitmap.HorizontalResolution, bitmap.VerticalResolution, PixelFormats.Bgr24,
										null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
			bitmap.UnlockBits(bitmapData);
			return bitmapSource;
		}

		private Rectangle ExtractRect(ResultPoint[] oResultPoints)
		{
			if(!(oResultPoints?.Any() ?? true))
				return new Rectangle();

			var rect = new Rectangle((int)oResultPoints[0].X, (int)oResultPoints[0].Y, 1, 1);
			foreach(var oPoint in oResultPoints)
			{
				if(oPoint.X < rect.Left)
					rect = new Rectangle((int)oPoint.X, rect.Y, rect.Width + rect.X - (int)oPoint.X, rect.Height);
				if(oPoint.X > rect.Right)
					rect = new Rectangle(rect.X, rect.Y, rect.Width + (int)oPoint.X - rect.X, rect.Height);
				if(oPoint.Y < rect.Top)
					rect = new Rectangle(rect.X, (int)oPoint.Y, rect.Width, rect.Height + rect.Y - (int)oPoint.Y);
				if(oPoint.Y > rect.Bottom)
					rect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height + (int)oPoint.Y - rect.Y);
			}

			return rect;
		}

		private void TryDecode(Bitmap oFrame)
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

				if(oResults?.Any() ?? false)
				{
					var oResult = oResults.Select(x => new
					{
						ResultString = x.ToString(),
						Rectangle = ExtractRect(x.ResultPoints)
					});

					OnResultFound(new BarcammerEventArgs(oResult.Select(x => x.ResultString).ToArray(),
														 oResult.Select(x => x.Rectangle).ToArray()));
				}
			}
			catch(Exception)
			{
			}
		}
	}
}
