using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script
{
	public class FileScriptResource : ScriptResource
	{
		public string DebugPath { get; set; }
		public string MinPath { get; set; }

		/// <summary>
		/// A .js script file
		/// </summary>
		/// <param name="name"></param>
		/// <param name="minPath"></param>
		/// <param name="debugPath"></param>
		/// <param name="dependencyNames"></param>
		public FileScriptResource(string name, string debugPath, string minPath, string[] dependencyNames)
			: base(name, dependencyNames)
		{
			DebugPath = debugPath;
			MinPath = minPath;
		}
	}
}
