using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Barcam;

namespace BarcodeWebcamCS
{
	public partial class BarcodeWebcamTestForm : Form
    {
        private Barcammer oBarcam;
		private IEnumerable<Rectangle> oCurrentResultRects;
		private readonly Pen oResultRectPen = new Pen(Color.OrangeRed);

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
            oBarcam.SetDevice((string)comboBox1.SelectedItem).StartWebcamBarcodeCapture();

            oBarcam.ResultFound += oBarcam_ResultFound;
            oBarcam.BitmapChanged += oBarcam_BitmapChanged;
        }

        private void oBarcam_BitmapChanged(object sender, Barcammer.BitmapEventArgs e)
        {
            try
            {
				pictureBox1.Width = e.Bitmap.Width;
				pictureBox1.Height = e.Bitmap.Height;

				pictureBox1.Image = e.Bitmap;
			}
            catch(Exception)
            {

            }
        }

		private void oBarcam_ResultFound(object sender, Barcammer.BarcammerEventArgs e)
		{
			oCurrentResultRects = e.ResultRects;
			Invoke(new Action(() => oLabel.Text = e.Results.Aggregate((s1, s2) => $@"{s1}, {s2}")));
		}

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) => oBarcam.Dispose();


		private void pictureBox1_Paint(object sender, PaintEventArgs e)
		{
			if(oCurrentResultRects == null || !oCurrentResultRects.Any())
				return;

			foreach(var oRect in oCurrentResultRects)
				e.Graphics.DrawRectangle(oResultRectPen, oRect);

			oCurrentResultRects = null;
		}
	}
}
