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

			if (currentState != RecordState.Stop)
			{
				currentFaceGray = grayFrame.Copy(face).Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);
				currentFaceGray._EqualizeHist();
				pictureBox2.Image = currentFaceGray.ToBitmap();
			}

			frame.Draw(face, new Bgr(Color.Red), 2);

			switch (currentState)
			{
				case RecordState.Rec:
					faces.Add(currentFaceGray);
					currentState = RecordState.Stop;
					break;

				case RecordState.RecMulty:
					faces.Add(currentFaceGray);
					if (faces.Count == MULTY_FACE_COUNT)
					{
						currentState = RecordState.Stop;
					}
					break;
				default:
				case RecordState.Stop:
					break;
			}

			pictureBox1.Image = frame.ToBitmap();
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

		private void Clear()
		{
			faces = new List<Image<Gray, byte>>();
			currentFaceGray = null;
			pictureBox2.Image = null;
		}

		private void AddFace(Image<Gray, Byte> face, string name)
		{
			string imagePath = RandomImageName(name);

			if (!Directory.Exists(Recognizer.TRAINED_FACES_PATH))
			{
				Directory.CreateDirectory(Recognizer.TRAINED_FACES_PATH);
			}

			while (File.Exists(imagePath))
			{
				imagePath = RandomImageName(name);
			}

			face.ToBitmap().Save(imagePath);

			string facesDataPath = Recognizer.TRAINED_FACES_PATH + "faces.xml";

			XmlDocument document = new XmlDocument();

			bool loaded = false;
			try
			{
				document.Load(facesDataPath);
				loaded = true;
			}
			catch
			{
				document = new XmlDocument();
			}

			if (!loaded)
			{
				using (XmlWriter writer = document.CreateNavigator().AppendChild())
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("faces");
					writer.WriteEndElement();
					writer.WriteEndDocument();
				}
			}

			XmlElement rootNode = document.DocumentElement;
			XmlElement faceNode = document.CreateElement("face");
			XmlElement nameNode = document.CreateElement("name");
			XmlElement fileNode = document.CreateElement("file");

			nameNode.InnerText = name;
			fileNode.InnerText = Path.GetFileName(imagePath);

			faceNode.AppendChild(nameNode);
			faceNode.AppendChild(fileNode);

			rootNode.AppendChild(faceNode);

			document.Save(facesDataPath);
		}

		private string RandomImageName(string name)
		{
			Random random = new Random();
			return Recognizer.TRAINED_FACES_PATH + "face_" + name + "_" + random.Next().ToString() + ".jpg";
		}

		private int Clamp(int currentValue, int min, int max)
		{
			return currentValue <= min ? min : (currentValue >= max ? max : currentValue);
		}

		private void btnPrev_Click(object sender, EventArgs e)
		{
			if (currentState != RecordState.Stop || faces.Count == 0 || currentFacePos <= 0)
			{
				return;
			}

			currentFaceGray = faces[--currentFacePos];
			pictureBox2.Image = currentFaceGray.ToBitmap();
		}

		private void btnNext_Click(object sender, EventArgs e)
		{
			if (currentState != RecordState.Stop || faces.Count == 0 || currentFacePos >= faces.Count - 1)
			{
				return;
			}

			currentFaceGray = faces[++currentFacePos];
			pictureBox2.Image = currentFaceGray.ToBitmap();
		}

		private void btn_Clear_Click(object sender, EventArgs e)
		{
			Clear();
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			if (currentState != RecordState.Stop || currentFaceGray == null || string.IsNullOrEmpty(textBox1.Text))
			{
				return;
			}

			AddFace(currentFaceGray, textBox1.Text);

			faces.Remove(currentFaceGray);

			if (faces.Count > 0)
			{
				currentFacePos = Clamp(currentFacePos, 0, faces.Count);
				currentFaceGray = faces[currentFacePos];
				pictureBox2.Image = currentFaceGray.ToBitmap();
			}
			else
			{
				Clear();
			}
		}

		private void btnAddAll_Click(object sender, EventArgs e)
		{
			if (currentState != RecordState.Stop || faces.Count == 0)
			{
				return;
			}

			while (faces.Count > 0) {
				AddFace(faces[0], textBox1.Text);
				faces.RemoveAt(0);
			}

			Clear();
		}

		private void btnRec_Click(object sender, EventArgs e)
		{
			Clear();
			currentState = RecordState.Rec;
		}

		private void btnRecMulty_Click(object sender, EventArgs e)
		{
			Clear();
			currentState = RecordState.RecMulty;
		}
	}
}
