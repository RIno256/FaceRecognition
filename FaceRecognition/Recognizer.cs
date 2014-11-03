using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace FaceRecognition
{
	class Recognizer
	{
		public bool IsTrained
		{
			get;
			private set;
		}

		public string Recognize(Image<Gray, Byte> image)
		{
			return "";
		}
	}
}
