using Barcam;
using System;
using System.Linq;
using System.Windows.Forms;

namespace BarcodeWebcamCS
{
    public partial class BarcodeWebcamTestForm : Form
    {
        private Barcammer oBarcam;

        public BarcodeWebcamTestForm() => InitializeComponent();

        private void Form1_Load(object sender, EventArgs e)
        {
            oBarcam = Barcammer.Create();

            comboBox1.Items.AddRange(oBarcam.DeviceNames.ToArray());

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            oBarcam.SetDevice((string)comboBox1.SelectedItem);
            oBarcam.StartWebcamBarcodeCapture();

            oBarcam.ResultFound += oBarcam_ResultFound;
            oBarcam.BitmapChanged += OBarcam_BitmapChanged;
        }

        private void OBarcam_BitmapChanged(object sender, Barcammer.BitmapEventArgs e)
        {
            try
            {
                pictureBox1.Image = e.Bitmap;
            }
            catch(Exception)
            {

            }
        }

        private void oBarcam_ResultFound(object sender, Barcammer.BarcammerEventArgs e) => oLabel.Invoke(new Action(() => oLabel.Text = e.Result));
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) => oBarcam.Dispose();
    }
}
