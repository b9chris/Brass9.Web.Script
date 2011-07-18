using System;
using System.Collections.Generic;


namespace Brass9.Web.Style
{
	public class AppCss
	{
		protected static AppCss current;
		public static AppCss Current
		{
			get
			{
				// We don't need to worry about multithreading here since this should either be initialized
				// in Global.asax.cs, a test harness, or not at all
				if (current == null)
					current = new AppCss();

				return current;
			}

			set
			{
				// Allow overriding for tests (overwrite with MockAppScripts)
				current = value;
			}
		}


		public Dictionary<string, CssResource> List = new Dictionary<string, CssResource>();

		protected string cssFolder = "/content/";
		public string CssFolder
		{
			get { return cssFolder; }
			set { cssFolder = value; }
		}

		public AppCss()
		{
		}

		public void Add(string name, string debugPath, string minPath)
		{
			List.Add(name, new FileCssResource(name, debugPath, minPath));
		}
	}
}
