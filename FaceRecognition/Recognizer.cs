using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.Util;

namespace FaceRecognition
{
	struct RecognizeResult
	{
		private string name;
		private double distance;

		public string Name
		{
			get
			{
				return name;
			}

			set
			{
				name = value;
			}
		}

		public double Distance
		{
			get
			{
				return distance;
			}
			set
			{
				distance = value;
			}
		}
	}
	class Recognizer : IDisposable
	{
		public static readonly string TRAINED_FACES_PATH = Path.Combine(Application.StartupPath, "TrainedFaces\\");
		private LBPHFaceRecognizer recognizer = null;

		private List<Image<Gray, byte>> faces = new List<Image<Gray, byte>>();
		private List<string> names = new List<string>();

		public Recognizer()
		{
			IsTrained = Train(TRAINED_FACES_PATH);
		}

		public bool IsTrained
		{
			get;
			private set;
		}

		public RecognizeResult Recognize(Image<Gray, Byte> image)
		{
			RecognizeResult result = new RecognizeResult();

			if (!IsTrained)
			{
				return result;
			}

			FaceRecognizer.PredictionResult predictionResult = recognizer.Predict(image);

			if (predictionResult.Label == -1)
			{
				result.Name = "unknown";
				return result;
			}

			result.Name = names[predictionResult.Label];
			result.Distance = predictionResult.Distance;

			return result;
		}

		public bool Retrain()
		{
			return IsTrained = Train(TRAINED_FACES_PATH);
		}

		public void Save(string filename)
		{
			recognizer.Save(filename);
			string path = Path.GetDirectoryName(filename);
			FileStream labels = File.OpenWrite(Path.Combine(path, "Labels.xml"));
			using (XmlWriter writer = XmlWriter.Create(labels))
			{
				writer.WriteStartDocument();
				writer.WriteStartElement("labels");

				for (int i = 0; i < names.Count; ++i)
				{
					writer.WriteStartElement("label");
					writer.WriteElementString("name", names[i]);
					writer.WriteEndElement();
				}

				writer.WriteEndElement();
				writer.WriteEndDocument();
			}

			labels.Close();
		}

		public void Load(string filename)
		{
			recognizer.Load(filename);


			string path = Path.GetDirectoryName(filename);
			FileStream labels = File.OpenRead(Path.Combine(path, "Labels.xml"));
			names.Clear();

			using (XmlReader reader = XmlTextReader.Create(labels))
			{
				while (reader.Read())
				{
					if (reader.IsStartElement())
					{
						switch (reader.Name)
						{
							case "name":
								names.Add(reader.GetAttribute("name"));
								break;
						}
					}
				}
			}

			labels.Close();

			IsTrained = true;
		}

		public void Dispose()
		{
			recognizer.Dispose();
			faces = null;
			names = null;

			GC.Collect();
		}

		private bool Train(string folder)
		{
			string facesPath = Path.Combine(folder, "faces.xml");
			if (!File.Exists(facesPath))
			{
				return false;
			}

			try
			{
				names.Clear();
				faces.Clear();
				List<int> tmp = new List<int>();
				FileStream facesInfo = File.OpenRead(facesPath);

				using (XmlReader reader = XmlTextReader.Create(facesInfo))
				{
					while (reader.Read())
					{
						if (reader.IsStartElement())
						{
							switch (reader.Name)
							{
								case "name":
									if (reader.Read())
									{
										tmp.Add(names.Count);
										names.Add(reader.Value.Trim());
									}
									break;
								case "file":
									if (reader.Read())
									{
										faces.Add(new Image<Gray, byte>(Path.Combine(Application.StartupPath,
											"TrainedFaces",
											reader.Value.Trim())));
									}
									break;
							}
						}
					}
				}

				facesInfo.Close();

				if (faces.Count == 0)
				{
					return false;
				}

				recognizer = new LBPHFaceRecognizer(1, 8, 8, 8, 100);
				recognizer.Train(faces.ToArray(), tmp.ToArray());

				return true;
			}
			catch
			{
			    return false;
			}
		}
	}
}
