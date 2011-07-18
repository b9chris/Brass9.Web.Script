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
			PageCssList.Add(AppCss.Current.List[name]);
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
					writer.Write("<link href=\"");
					writer.Write(appCss.CssFolder);
					writer.Write(file.DebugPath);
					writer.WriteLine("\" rel=stylesheet />");
#else
					writer.Write("<link href=\"");
					writer.Write(appCss.CssFolder);
					writer.Write(file.MinPath);
					writer.WriteLine("\" rel=stylesheet />");
#endif
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
			var file = (FileCssResource)css;
#if DEBUG
			writer.Write("<link href=\"");
			writer.Write(AppCss.Current.CssFolder);
			writer.Write(file.DebugPath);
			writer.WriteLine("\" rel=stylesheet />");
#else
			writer.Write("<link href=\"");
			writer.Write(AppCss.Current.CssFolder);
			writer.Write(file.MinPath);
			writer.WriteLine("\" rel=stylesheet />");
#endif
		}


		public void RenderCssInline(TextWriter writer, CssResource css, bool includeStyleTags)
		{
			var file = (FileCssResource)css;
#if DEBUG
			string webPath = AppCss.Current.CssFolder + file.DebugPath;
#else
			string webPath = AppCss.Current.CssFolder + file.MinPath;
#endif
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
