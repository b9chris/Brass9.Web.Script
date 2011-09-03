using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;

namespace Brass9.Web.Style
{
	public class PageCss
	{
		protected static PageCss current;
		public static PageCss Current
		{
			get
			{
				if (current == null)
					current = new PageCss();

				return current;
			}
		}

		public List<CssResource> PageCssList
		{
			get
			{
				var list = (List<CssResource>)HttpContext.Current.Items["pageCssList"];
				if (list == null)
					HttpContext.Current.Items["pageCssList"] = list = new List<CssResource>();

				return list;
			}
		}

		/// <summary>
		/// Includes a CSS file declared in Global.asax, by name, into the page
		/// </summary>
		/// <param name="name"></param>
		public void Include(string name)
		{
			CssResource cssFile;
			if (AppCss.Current.List.TryGetValue(name, out cssFile))
			{
				PageCssList.Add(cssFile);
				return;
			}

			throw new ArgumentOutOfRangeException("name", name, "Did you remember to define this Stylesheet in Global.asax.cs?");
		}

		/// <summary>
		/// Includes a CSS file that's not declared in Global.asax into the page
		/// </summary>
		/// <param name="debugPath"></param>
		/// <param name="minPath"></param>
		public void Include(string debugPath, string minPath)
		{
			PageCssList.Add(new FileCssResource("", debugPath, minPath));
		}

		public void Render(TextWriter writer)
		{
			var appCss = AppCss.Current;

			writer.WriteLine();

			foreach (CssResource css in PageCssList)
			{
				// No inline support for now
				//if (css is FileCssResource)
				//{
					var file = (FileCssResource)css;

#if DEBUG
					string cssFilePath = file.DebugPath;
#else
					string cssFilePath = file.MinPath;
#endif

					writer.Write("<link href=\"");
					if (!cssFilePath.StartsWith("http") && !cssFilePath.StartsWith("/"))
						writer.Write(appCss.CssFolder);
					writer.Write(cssFilePath);
					writer.WriteLine("\" rel=stylesheet />");
				//}
			}
		}

		public string Render()
		{
			StringWriter writer = new StringWriter();
			Render(writer);
			return writer.ToString();
		}


		public void RenderCss(TextWriter writer, CssResource css)
		{
			string cssFolder = AppCss.Current.CssFolder;
			var file = (FileCssResource)css;

#if DEBUG
			string cssFilePath = file.DebugPath;
#else
			string cssFilePath = file.MinPath;
#endif

			writer.Write("<link href=\"");
			if (!cssFilePath.StartsWith("http") && !cssFilePath.StartsWith("/"))
				writer.Write(cssFolder);
			writer.Write(cssFilePath);
			writer.WriteLine("\" rel=stylesheet />");
		}


		public void RenderCssInline(TextWriter writer, CssResource css, bool includeStyleTags)
		{
			var file = (FileCssResource)css;
#if DEBUG
			string webPath = file.DebugPath;
#else
			string webPath = file.MinPath;
#endif

			if (!webPath.StartsWith("http") && !webPath.StartsWith("/"))
				webPath = AppCss.Current.CssFolder + webPath;

			string filePath = HttpContext.Current.Server.MapPath("~" + webPath);
			var fileInfo = new FileInfo(filePath);

			if (includeStyleTags)
				writer.WriteLine("<style>");

			using (StreamReader reader = fileInfo.OpenText())
			{
				string line;

				while ((line = reader.ReadLine()) != null)
					writer.WriteLine(line);
			}

			if (includeStyleTags)
				writer.WriteLine("</style>");
		}
	}
}
