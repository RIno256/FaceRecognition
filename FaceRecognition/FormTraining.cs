using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

using Emgu.CV;
using Emgu.CV.Structure;

namespace FaceRecognition
{
	public partial class FormTraining : Form
	{
		private const int MULTY_FACE_COUNT = 10;

		private enum RecordState
		{
			Rec,
			RecMulty,
			Stop
		}

		private Capture capture = null;
		private CascadeClassifier haar = null;

		private Image<Bgr, Byte> frame = null;
		private Image<Gray, Byte> grayFrame = null;
		private Image<Gray, Byte> currentFaceGray = null;

		private List<Image<Gray, Byte>> faces = new List<Image<Gray, byte>>();
		private int currentFacePos = 0;
		private RecordState currentState = RecordState.Stop;

		public FormTraining(CascadeClassifier haarFace)
		{
			InitializeComponent();
			haar = haarFace;

			InitializeCapture();
		}

		public void InitializeCapture()
		{
			capture = new Capture();

			Application.Idle += ProcessFrame;
		}

		public void StopCapture()
		{
			Application.Idle -= ProcessFrame;
			capture.Dispose();
		}

		private void ProcessFrame(object sender, EventArgs e)
		{
			frame = capture.QueryFrame().ToImage<Bgr, Byte>().Resize(320, 240, Emgu.CV.CvEnum.Inter.Cubic);

			if (currentState == RecordState.Stop)
			{
				pictureBox1.Image = frame.ToBitmap();
				return;
			}

			grayFrame = frame.Convert<Gray, Byte>();

			Rectangle[] facesDetected = haar.DetectMultiScale(grayFrame, 1.2, 10, new Size(50, 50), Size.Empty);

			if (facesDetected == null || facesDetected.Length == 0)
			{
				return;
			}

			Rectangle face = facesDetected[0];

			face.X += (int)(face.Height * 0.15);
			face.Y += (int)(face.Width * 0.22);
			face.Height -= (int)(face.Height * 0.3);
			face.Width -= (int)(face.Width * 0.35);

			currentFaceGray = grayFrame.Copy(face).Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);
			currentFaceGray._EqualizeHist();

			frame.Draw(face, new Bgr(Color.Red), 2);

			pictureBox2.Image = currentFaceGray.ToBitmap();

			switch (currentState)
			{
				case RecordState.Rec:
					currentState = RecordState.Stop;
					break;

				case RecordState.RecMulty:
					faces.Add(currentFaceGray);
					if (faces.Count == MULTY_FACE_COUNT)
					{
						currentState = RecordState.Stop;
					}
					break;
			}
		}

		private void ReleaseData()
		{
			if (capture != null)
			{
				StopCapture();
			}
		}

		private void FormTraining_FormClosing(object sender, FormClosingEventArgs e)
		{
			FormRecognizer.Instance.InitializeCapture();
		}
	}
}
