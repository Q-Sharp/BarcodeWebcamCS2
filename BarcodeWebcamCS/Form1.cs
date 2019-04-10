using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using AForge;
using AForge.Video;
using ZXing;
using ZXing.Aztec;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video.DirectShow;

namespace BarcodeWebcamCS
{
    public partial class Form1 : Form
    {
        private VideoCaptureDevice FinalFrame;
        private FilterInfoCollection CaptureDevice;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CaptureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo Device in CaptureDevice)
            {
                comboBox1.Items.Add(Device.Name);
            }

            if(comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
            FinalFrame = new VideoCaptureDevice();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FinalFrame = new VideoCaptureDevice(CaptureDevice[comboBox1.SelectedIndex].MonikerString);
            FinalFrame.NewFrame += new NewFrameEventHandler(FinalFrame_NewFrame);
            FinalFrame.Start();

        }

        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var oReader = new BarcodeReader();
            oReader.AutoRotate = true;
            oReader.Options.TryHarder = true;
            var oResult = oReader.Decode((Bitmap)pictureBox1.Image);

            try
            {
                
                var decoded = oResult.ToString().Trim();
                if (decoded != "")
                {                   
                    oLabel.Text = decoded;

                    var myPen = new Pen(Color.Black);
                    var formGraphics = this.CreateGraphics();

                    var x = (int)oResult.ResultPoints[0].X;
                    var y = (int)oResult.ResultPoints[0].Y;

                    var w = (int)oResult.ResultPoints[1].X - (int)oResult.ResultPoints[0].X;
                    var h = (int)oResult.ResultPoints[1].Y - (int)oResult.ResultPoints[0].Y;

                    formGraphics.DrawRectangle(myPen, new Rectangle(x, y, w, h));
                    myPen.Dispose();
                    formGraphics.Dispose();
                }
            }
            catch (Exception)
            {

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            timer1.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (FinalFrame.IsRunning == true)
            {
                FinalFrame.Stop();
            }
        }
    }
}
