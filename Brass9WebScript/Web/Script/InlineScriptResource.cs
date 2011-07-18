using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script
{
	public class InlineScriptResource : ScriptResource
	{
		public string Body;

		/// <summary>
		/// A function whose body is in the Body property, like:
		/// .Body = @"var x = Math.sqrt(6)";
		/// The contents of Body will get pushed into an inline function - the code output
		/// on the page will look like:
		/// 
		/// function() {
		///   var x = Math.sqrt(6);
		/// }
		/// </summary>
		/// <param name="name"></param>
		/// <param name="body"></param>
		/// <param name="dependencyNames"></param>
		public InlineScriptResource(string name, string body, string[] dependencyNames)
			: base(name, dependencyNames)
		{
			Body = body;
		}
	}
}
