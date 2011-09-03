using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;


namespace Brass9.Web.Script
{
	public enum ScriptsRenderMode { Async, Simple };

	public class Scripts
	{
		protected static Scripts current;
		public static Scripts Current
		{
			get
			{
				// Don't worry about this being set by multiple threads - we don't care.
				// There aren't any instance variables to worry about - everything in
				// this implementation lives in the Request (PageScripts and
				// InlineScriptCounter), and a Request is serviced by a single thread
				// anyway.
				// This is basically a static class, but we use the Singleton pattern
				// to make it easy to mock and test.
				if (current == null)
					current = new Scripts();

				return current;
			}

			set
			{
				current = value;
			}
		}


		public virtual HashSet<ScriptResource> PageScripts
		{
			get
			{
				HashSet<ScriptResource> pageScripts = (HashSet<ScriptResource>)HttpContext.Current.Items["pageScripts"];
				if (pageScripts == null)
					HttpContext.Current.Items["pageScripts"] = pageScripts = new HashSet<ScriptResource>();

				return pageScripts;
			}
		}

		public virtual bool Debug
		{
			get
			{
				object item = HttpContext.Current.Items["pageScriptsDebug"];
				if (item == null)
					return AppScripts.Current.Debug;
				
				return (bool)item;
			}

			set
			{
				HttpContext.Current.Items["pageScriptsDebug"] = value;
			}
		}

		public ScriptsRenderMode RenderMode
		{
			get
			{
				object item = HttpContext.Current.Items["pageScriptsRenderMode"];
				if (item == null)
#if DEBUG
					return ScriptsRenderMode.Simple;
#else
					return ScriptsRenderMode.Async;
#endif

				return (ScriptsRenderMode)item;
			}

			set
			{
				HttpContext.Current.Items["pageScriptsRenderMode"] = value;
			}
		}

		public virtual Int32 InlineScriptCounter
		{
			get
			{
				if (HttpContext.Current.Items["inlineScriptCounter"] == null)
				{
					HttpContext.Current.Items["inlineScriptCounter"] = (Int32)0;
					return 0;
				}

				return (Int32)HttpContext.Current.Items["inlineScriptCounter"];
			}

			set
			{
				HttpContext.Current.Items["inlineScriptCounter"] = value;
			}
		}

		public virtual Int32 PageonlyScriptCounter
		{
			get
			{
				if (HttpContext.Current.Items["pageonlyScriptCounter"] == null)
				{
					HttpContext.Current.Items["pageonlyScriptCounter"] = (Int32)0;
					return 0;
				}

				return (Int32)HttpContext.Current.Items["pageonlyScriptCounter"];
			}

			set
			{
				HttpContext.Current.Items["pageonlyScriptCounter"] = value;
			}
		}


		/// <summary>
		/// Include a script or scripts in the outgoing View by friendly name.
		/// 
		/// Will be loaded according to its dependencies declared
		/// with Brass9.Web.Script.AppScripts.Add() in Global.asax.cs;
		/// if no dependencies are declared for example, the script
		/// will be loaded in parallel with all other scripts.
		/// </summary>
		/// <param name="name">Name of script to include</param>
		public ScriptResource Include(string name)
		{
			ScriptResource script;
			if (AppScripts.Current.List.TryGetValue(name, out script))
			{
				PageScripts.Add(script);
				return script;
			}

			throw new ArgumentOutOfRangeException("name", name, "Did you remember to define this Script in Global.asax.cs?");
		}

		/// <summary>
		/// Include a script or scripts in the outgoing View by friendly name.
		/// 
		/// Will be loaded according to its dependencies declared
		/// with Brass9.Web.Script.AppScripts.Add() in Global.asax.cs;
		/// if no dependencies are declared for example, the script
		/// will be loaded in parallel with all other scripts.
		/// </summary>
		/// <param name="name">An array of scripts to include</param>
		public void Include(string[] scripts)
		{
			foreach (string script in scripts)
				Include(script);
		}

		/// <summary>
		/// Include a script in the outgoing View by path, instead of pre-defining
		/// it in Global.asax.cs and referencing it by name.
		/// 
		/// Useful for page-only scripts that you don't want to take the time to
		/// declare more broadly for the app.
		/// </summary>
		public void Include(string name, string debugPath, string minPath, string dependencies)
		{
			FileScriptResource script = new FileScriptResource(name, debugPath, minPath, splitCommaDelimitedString(dependencies));
			PageScripts.Add(script);
		}



		protected string[] splitCommaDelimitedString(string str)
		{
			if (str == null)
				return null;

			return new Regex(", ?").Split(str);
		}

		/// <summary>
		/// Add a script with optional dependencies to the load sequence.
		/// </summary>
		/// <param name="codeChunkName">A name, in case you want to have other scripts depend on this one</param>
		/// <param name="code">The JS. No script tags. The code will run inside of a (function(){...}()) meaning any variables are isolated from
		/// other scripts on the page</param>
		/// <param name="dependencies">Optional. Scripts this code depends on. Pass null if none.</param>
		public void RunJsAfter(string codeChunkName, string code, string dependencies)
		{
			InlineScriptResource script = new InlineScriptResource(codeChunkName, code, splitCommaDelimitedString(dependencies));
			PageScripts.Add(script);
		}

		/// <summary>
		/// Add a script with optional dependencies to the load sequence.
		/// </summary>
		/// <param name="code">The JS. No script tags. The code will run inside of a (function(){...}()) meaning any variables are isolated from
		/// other scripts on the page</param>
		/// <param name="dependencies">Optional. Scripts this code depends on. Pass null if none.</param>
		public void RunJsAfter(string code, string dependencies)
		{
			// Generate a name for this code chunk, to prevent collisions, and play nice with the
			// system in general.
			int counter = InlineScriptCounter++;
			InlineScriptResource script = new InlineScriptResource("inline" + counter, code, splitCommaDelimitedString(dependencies));
			PageScripts.Add(script);
		}

		/// <summary>
		/// Writes out scripts, taking dependency into account.
		/// Uses LabJs to do so in the most efficient manner possible.
		/// 
		/// Call this at the bottom of a View, right before the closing body
		/// tag. Calling it anywhere else could cause for example child
		/// PartialViews to declare scripts that are never rendered, if those
		/// Partials are rendered after this call.
		/// </summary>
		public void Render(TextWriter writer)
		{
			// If there are no scripts, render nothing - we're done here.
			if (PageScripts.Count == 0)
				return;

			// TODO: Put this mapping step out on a BeginMapScripts method for
			// pages that want to kick this step off then get other logic done
			// TODO: Make PageScriptMapper swappable with other mapper classes
			// others might want to write
			//PageScriptMapper mapper = new PageScriptMapper();
			//HashSet<ScriptLoadGroup> scriptMap = mapper.MapScripts(PageScripts);
			TreeMapper.PageScriptTreeMapper mapper = new TreeMapper.PageScriptTreeMapper();
			HashSet<ScriptLoadGroup> scriptMap = mapper.MapScripts(PageScripts, AppScripts.Current.List);

			var appScripts = AppScripts.Current;

			if (RenderMode == ScriptsRenderMode.Async)
			{
				renderAsync(writer, scriptMap, appScripts);
			}
			else // if (renderMode == ScriptsRenderMode.Simple)
			{
				// Write things out in a way that's very simple, isolated, and easy to read
				renderSimple(writer, scriptMap, appScripts);
			}
		}

		public string Render()
		{
			StringWriter writer = new StringWriter();
			Render(writer);
			return writer.ToString();
		}

		protected delegate void WriteDelegate(string str);


		protected void renderSimple(TextWriter writer, HashSet<ScriptLoadGroup> scriptMap, AppScripts appScripts)
		{
			foreach (ScriptLoadGroup group in scriptMap)
			{
				foreach (ScriptLoadItem item in group.Scripts)
				{
					renderSimpleScript(item, writer, appScripts);
				}

				ScriptLoadGroup childGroup = group.ChildGroup;
				while (childGroup != null)
				{
					foreach (ScriptLoadItem item in childGroup.Scripts)
					{
						renderSimpleScript(item, writer, appScripts);
					}

					childGroup = childGroup.ChildGroup;
				}
			}
		}

		protected void renderSimpleScript(ScriptLoadItem item, TextWriter writer, AppScripts appScripts)
		{
			if (item.Script is InlineScriptResource)
			{
				var script = (InlineScriptResource)item.Script;
				writer.WriteLine("<script>");
				writer.WriteLine("(function() {");
				writer.WriteLine(script.Body);
				writer.WriteLine("})();");
				writer.WriteLine("</script>");
			}
			else
			{
				var script = (FileScriptResource)item.Script;
				writer.Write("<script src=\"");

				writeScriptPath(script, appScripts, delegate(string str)
				{
					writer.Write(str);
				});

				writer.WriteLine("\"></script>");
			}
		}

		protected void renderAsync(TextWriter writer, HashSet<ScriptLoadGroup> scriptMap, AppScripts appScripts)
		{
			string labJsPath =  Debug ? appScripts.LabJsSrc : appScripts.LabJsMin;

			writer.Write("<script src=");
			if (labJsPath.Contains(" "))
				writer.Write("\"");

			if (!(labJsPath.StartsWith("http") || labJsPath.StartsWith("/")))
				writer.Write(appScripts.ScriptsFolder);

			writer.Write(labJsPath);

			if (labJsPath.Contains(" "))
				writer.Write("\"");
			writer.WriteLine("></script>");
			writer.WriteLine("<script>");
			writer.Write("$LAB.setGlobalDefaults({AppendTo:'body'});");

			foreach (ScriptLoadGroup group in scriptMap)
			{
				System.Diagnostics.Debug.Assert(group.Parent == null,
					"Root ScriptLoadGroup has a parent, meaning it's not actually at the root!");

				StringBuilder inlineScriptSB = new StringBuilder();
				StringBuilder fileScriptSB = new StringBuilder();

				// Batch up first group of inline scripts and file scripts
				foreach (ScriptLoadItem item in group.Scripts)
				{
					if (item.Script is InlineScriptResource)
					{
						// Get inline scripts out of the way - these are in the root
						// so we don't even bother with LabJs, we just call them.
						// Important to include them for concat/minify functionality.
						InlineScriptResource script = (InlineScriptResource)item.Script;
						inlineScriptSB.AppendLine();

						// Function wrapper usually pointless if var keyword not used
						// TODO: Handle cases where no vars declared in code that needs
						// to be wrapped
						//bool wrap = script.Body.Contains("var");
						//if (wrap)
							inlineScriptSB.AppendLine("(function(){");
						inlineScriptSB.AppendLine(script.Body);
						//if (wrap)
							inlineScriptSB.Append("})();");
					}
					else
					{
						// Start the dependency tree - fire off root includes
						FileScriptResource script = (FileScriptResource)item.Script;
						fileScriptSB.AppendLine();
						fileScriptSB.Append(".script('");

						writeScriptPath(script, appScripts, delegate(string str)
						{
							fileScriptSB.Append(str);
						});

						fileScriptSB.Append("')");
					}
				}

				// Write out first group of inline scripts outside LabJs
				writer.Write(inlineScriptSB.ToString());
				inlineScriptSB = null;

				// Start up LabJs
				writer.WriteLine();
				writer.Write("$LAB");

				// Write out first group of file scripts
				writer.Write(fileScriptSB.ToString());

				// Start digging through child groups
				ScriptLoadGroup childGroup = group.ChildGroup;
				while (childGroup != null)
				{
					fileScriptSB = new StringBuilder();

					List<InlineScriptResource> inlineScripts = new List<InlineScriptResource>();

					// Batch up inline scripts, and begin building file scripts string
					foreach (ScriptLoadItem item in childGroup.Scripts)
					{
						if (item.Script is InlineScriptResource)
						{
							InlineScriptResource script = (InlineScriptResource)item.Script;
							inlineScripts.Add(script);
						}
						else
						{
							FileScriptResource script = (FileScriptResource)item.Script;
							fileScriptSB.AppendLine();
							fileScriptSB.Append(".script('");

							writeScriptPath(script, appScripts, delegate(string str)
							{
								fileScriptSB.Append(str);
							});

							fileScriptSB.Append("')");
						}
					}



					writer.WriteLine();

					// Write out inline scripts, if any, in a wait() statement so they
					// load after their dependencies as requested
					if (inlineScripts.Count == 0)
					{
						// No inline scripts - just a plain wait statement to separate
						// the previous round of includes from the scripts waiting in
						// fileScriptsSB
						writer.Write(".wait()");
					}
					else
					{
						// TODO: Get fancier and try to notice a wrapper function and toss it as well,
						// probably requires something like Rhino or the DLR to really do right - some
						// parsing ourselves of the whole resulting tree after the fact and elimination of
						// useless code.
						// Actually, we should probably create this piece of script then put it through
						// ajaxmin -hc !

						// If it's just a function call, ditch the empty params and just pass the function
						// Looking for string like "fn();"
						if (inlineScripts.Count == 1)
						{
							if (new Regex(@"^[\w\d_-]+\(\);?$").IsMatch(inlineScripts[0].Body))
							{
								string body = inlineScripts[0].Body;
								// Writes out like .wait(fn)
								writer.Write(".wait(");
								writer.Write(body.Substring(0, body.IndexOf('(')));
								writer.Write(")");
							}
							else
							{
								writer.WriteLine(".wait(function(){");
								writer.WriteLine(inlineScripts[0].Body);
								writer.Write("})");
							}
						}
						else
						{
							writer.WriteLine(".wait(function(){");
							foreach (InlineScriptResource script in inlineScripts)
							{
								// Dump the body out like
								// (function(){
								// alert('hi');
								// })();
								writer.WriteLine("(function(){");
								writer.WriteLine(script.Body);
								writer.WriteLine("})();");
							}
							writer.Write("})");
						}
					}

					// Dump out the file scripts string we built earlier in the loop
					writer.Write(fileScriptSB.ToString());

					// On to the next child
					childGroup = childGroup.ChildGroup;
				}

				writer.WriteLine(";");
			}

			writer.WriteLine("</script>");
		}

		protected void writeScriptPath(FileScriptResource script, AppScripts appScripts, WriteDelegate write)
		{
			string path = Debug ? script.DebugPath : script.MinPath;

			// Don't append scripts folder if path looks like:
			// http://... https://... //www.blah... /folder/blah...
			if (!(path.StartsWith("http") || path.StartsWith("/")))
				write(appScripts.ScriptsFolder);

			write(path);
		}
	}
}
