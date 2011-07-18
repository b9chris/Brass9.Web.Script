using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Style
{
	public class FileCssResource : CssResource
	{
		public string DebugPath { get; set; }
		public string MinPath { get; set; }

		public FileCssResource(string name, string debugPath, string minPath)
		{
			Name = name;
			DebugPath = debugPath;
			MinPath = minPath;
		}
	}
}
