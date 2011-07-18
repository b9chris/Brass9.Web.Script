using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace Brass9.Web.Script
{
	/// <summary>
	/// Static methods that let you:
	/// Mark scripts with friendly names to be included in a website
	/// Define dependencies between scripts
	/// 
	/// The resulting apparatus can be used by classes like Brass9.Web.Mvc.Script.Scripts
	/// to render out all dependencies in a page in proper order, asynchronously.
	/// </summary>
	public class AppScripts
	{
		protected static AppScripts current;
		public static AppScripts Current
		{
			get
			{
				// We don't need to worry about multithreading here since this should either be initialized
				// in Global.asax.cs, a test harness, or not at all
				if (current == null)
					current = new AppScripts();

				return current;
			}

			set
			{
				// Allow overriding for tests (overwrite with MockAppScripts)
				current = value;
			}
		}

		public AppScripts()
		{
		}

		/// <summary>
		/// Declares a script in the application that any View or PartialView could ask for as
		/// an include or a dependency. Scripts are added by their friendly name (name parameter)
		/// to make it easy to switch the site between production and debug versions.
		/// </summary>
		/// <param name="name">Friendly name of the script to be used when calling it in Views</param>
		/// <param name="minPath">Path to production version. This can either be a minified remote script,
		/// typically on a CDN, like "http://ajax.microsoft.com/ajax/jQuery/jquery-1.4.2.min.js", or
		/// a Virtual Path to a local, non-minified script, like "~/Scripts/myscript.js"
		/// 
		/// Local scripts are sometimes concatenated then minified for users visiting with an empty cache,
		/// so minifying them prior is likely to be wasteful.
		/// 
		/// You can see a full list of scripts available on the Microsoft CDN here:
		/// http://www.asp.net/ajaxlibrary/cdn.ashx
		/// </param>
		/// <param name="debugPath">Path to debug version. Usually a local script, but can be a
		/// remote script if you prefer. Should not be minified.</param>
		/// <param name="dependencyArray">An array of friendly names of scripts this script
		/// depends on.</param>
		public void Add(string name, string debugPath, string minPath, string[] dependencyArray)
		{
			Dictionary<string, ScriptResource> scripts = List;

			if (scripts.ContainsKey(name))
				throw new ScriptAlreadyDefinedException(name);

			// TODO - verify that dependencyArray has all dependencies already declared?
			scripts.Add(name, new FileScriptResource(name, debugPath, minPath, dependencyArray));
		}

		public void Add(string name, string debugPath, string minPath, string dependencies)
		{
			Add(name, debugPath, minPath, new Regex(", ?").Split(dependencies));
		}

		public void Add(string name, string debugPath, string minPath)
		{
			Add(name, debugPath, minPath, (string[])null);
		}

		public virtual Dictionary<string, ScriptResource> List
		{
			get
			{
				Dictionary<string, ScriptResource> list = (Dictionary<string, ScriptResource>)HttpContext.Current.Application["scripts"];

				if (list == null)
					HttpContext.Current.Application["scripts"] = list = new Dictionary<string, ScriptResource>();

				return list;
			}
		}

// This default is set by the build, but we let you modify it at runtime in case
// you need to compile release C# and see some debug mode JS
#if DEBUG
		public bool Debug = true;
#else
		public bool Debug = false;
#endif

		/// <summary>
		/// Application-global setting for root path to where scripts live.
		/// Modify if scripts like LAB.src.js live elsewhere.
		/// Prepended to all script paths.
		/// Set to empty string if you don't want anything prepended to script paths.
		/// 
		/// Default: "/scripts/"
		/// 
		/// A prepend typically looks like:
		/// 
		/// "/scripts/" + "home/home.js"
		/// 
		/// If a path begins with / or http://, this string will not be prepended.
		/// </summary>
		public string ScriptsFolder = "/scripts/";

		/// <summary>
		/// Path to LabJs source.
		/// Default: "LAB.src.js"
		/// ScriptsFolder gets prepended to this path like all scripts, so the
		/// default location is "/scripts/" + "LAB.src.js"
		/// </summary>
		public string LabJsSrc = "LAB.src.js";

		/// <summary>
		/// Path to minified LabJs.
		/// Default: "LAB.min.js"
		/// ScriptsFolder gets prepended to this path like all scripts, so the
		/// default location is "/scripts/" + "LAB.min.js"
		/// </summary>
		public string LabJsMin = "LAB.min.js";


		public class ScriptAlreadyDefinedException : ArgumentException
		{
			public ScriptAlreadyDefinedException()
				: base()
			{ }

			public ScriptAlreadyDefinedException(string message)
				: base(message)
			{
			}
		}
	}
}
