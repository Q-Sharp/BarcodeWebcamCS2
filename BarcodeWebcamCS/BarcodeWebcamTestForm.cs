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
    public partial class BarcodeWebcamTestForm : Form
    {
        private VideoCaptureDevice CapureDevice;
        private FilterInfoCollection CaptureDevices;

        public BarcodeWebcamTestForm() => InitializeComponent();

        private void Form1_Load(object sender, EventArgs e)
        {
            CaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo Device in CaptureDevices)
                comboBox1.Items.Add(Device.Name);

            if(comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CapureDevice = new VideoCaptureDevice(CaptureDevices[comboBox1.SelectedIndex].MonikerString);
            CapureDevice.NewFrame += (s, a) => pictureBox1.Image = (Bitmap)a.Frame.Clone();
            CapureDevice.Start();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var oReader = new BarcodeReader
            {
                AutoRotate = true
            };
            var oResult = oReader.Decode((Bitmap)pictureBox1.Image);

            try
            {
                var decoded = oResult?.ToString()?.Trim();

                if (decoded != "")               
                    oLabel.Text = decoded;
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
            if (CapureDevice.IsRunning == true)
                CapureDevice.Stop();
        }
    }
}
