/*! Brass9.Web.Script
    v1.0.0 (c) Brass Nine Design
    MIT License
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Reflection;
using System.Text;
using System.IO;

using Brass9.Web.Script;
using Brass9.Web.Style;


namespace Brass9.Web.Mvc
{
	public static class MvcHtmlHelper
	{
		/// <summary>
		/// Include a script by name, added to AppScripts in Global.asax.cs
		/// Implicitly includes any defined dependencies.
		/// </summary>
		/// <param name="script">Name of script</param>
		public static ScriptResource IncludeScript(this HtmlHelper html, string script)
		{
			return Brass9.Web.Script.Scripts.Current.Include(script);
		}

		/// <summary>
		/// Include a list of scripts that have been added to AppScripts in Global.asax.cs
		/// Implicitly includes all of their dependencies.
		/// </summary>
		/// <param name="scripts">"script1", "script2", "script3"...</param>
		public static void IncludeScripts(this HtmlHelper html, params string[] scripts)
		{
			Brass9.Web.Script.Scripts.Current.Include(scripts);
		}

		/// <summary>
		/// Include a script in the outgoing View by path, instead of pre-defining
		/// it in Global.asax.cs and referencing it by name.
		/// 
		/// Useful for page-only scripts that you don't want to take the time to
		/// declare more broadly for the app.
		/// </summary>
		/// <param name="name">A name for this script that uniquely identifies it in the page,
		/// and allows other scripts to point to it as a dependency.</param>
		/// <param name="debugPath">The path to unminified source with comments. Note that this path
		/// will be appended to AppScripts.Current.ScriptsFolder - typically "/scripts/"</param>
		/// <param name="minPath">The path to minified source. Note that this path
		/// will be appended to AppScripts.Current.ScriptsFolder - typically "/scripts/"</param>
		/// <param name="dependencies">A comma-delimited string list of scripts this script
		/// depends on. The names can come from scripts defined in AppScripts (Global.asax.cs) and/or
		/// scripts defined on this page.</param>
		public static void IncludeScript(this HtmlHelper html, string name, string debugPath, string minPath, string dependencies)
		{
			Brass9.Web.Script.Scripts.Current.Include(name, debugPath, minPath, dependencies);
		}

		/// <summary>
		/// Run the specified JS code after a list of dependencies has loaded.
		/// If the codeChunkName has already been used in a previous call to
		/// RunJsAfter, this call has no effect. Use this version of RunJsAfter
		/// to guarantee uniqueness of the code chunk passed in.
		/// 
		/// Otherwise, see the 2 parameter version.
		/// </summary>
		/// <param name="codeChunkName">A unique name for this code chunk. Guarantees uniqueness.</param>
		public static void RunJsAfter(this HtmlHelper html, string codeChunkName, string code, string dependencies)
		{
			Brass9.Web.Script.Scripts.Current.RunJsAfter(codeChunkName, code, dependencies);
		}

		/// <summary>
		/// Run the specified JS code after a list of dependencies has loaded.
		/// 
		/// Example:
		/// 
		/// Html.RunJsAfter("$('#output').text('JQuery loaded.');", "jquery");
		/// 
		/// Will result in jquery loading, and then this being executed on the page:
		/// 
		/// (function(){
		/// $('#output').text('JQuery loaded.');
		/// })();
		/// 
		/// Note that if you place this statement in a loop, every time this is called,
		/// the code passed-in will be executed.
		/// 
		/// Example:
		/// 
		/// for(int i = 0; i &lt; 3; i++)
		///   Html.RunJsAfter("alert('hi');", "jquery");
		///   
		/// Results in alert('hi') being run 3 times on the page. This is important to
		/// consider when for example calling RunJsAfter from a PartialView, which will
		/// likely be executed multiple times within a larger template. If you want to
		/// guarantee that your code is run exactly 0 or 1 times, use the named version
		/// of RunJsAfter which takes 3 parameters.
		/// </summary>
		/// <param name="code">The js that should execute on the page after dependencies have loaded.</param>
		/// <param name="dependencies">The dependencies to download and run before executing this code.</param>
		public static void RunJsAfter(this HtmlHelper html, string code, string dependencies)
		{
			Brass9.Web.Script.Scripts.Current.RunJsAfter(code, dependencies);
		}


		/// <summary>
		/// Call this at the bottom of any page using the Brass9 Scripting framework.
		/// Typically called at the bottom of all MasterPages.
		/// 
		/// If no scripts have been included by any of the views, partialviews, etc,
		/// this call outputs nothing, so including this in a Master won't add extra
		/// junk to pages that are intentionally simple.
		/// </summary>
		public static void RenderScripts(this HtmlHelper html)
		{
			Brass9.Web.Script.Scripts.Current.Render(html.ViewContext.Writer);
		}


		/// <summary>
		/// Includes a CSS file declared in Global.asax into the page
		/// </summary>
		public static void IncludeCss(this HtmlHelper html, string name)
		{
			PageCss.Current.Include(name);
		}

		/// <summary>
		/// Includes a CSS file into this page's overall list of CSS files to be written out later by
		/// RenderCss
		/// </summary>
		public static void IncludeCss(this HtmlHelper html, string debugPath, string minPath)
		{
			PageCss.Current.Include(debugPath, minPath);
		}

		/// <summary>
		/// Renders all CSS files included by commands like IncludeCss.
		/// </summary>
		public static void RenderCss(this HtmlHelper html)
		{
			PageCss.Current.Render(html.ViewContext.Writer);
		}



		/// <summary>
		/// Immediately renders a CSS file inline into the page - does not wait for RenderCss
		/// </summary>
		/// <param name="debugPath"></param>
		/// <param name="minPath"></param>
		public static void RenderCssInlineNow(this HtmlHelper html, string debugPath, string minPath)
		{
			FileCssResource css = new FileCssResource(null, debugPath, minPath);
			PageCss.Current.RenderCssInline(html.ViewContext.Writer, css, true);
		}
	}
}
