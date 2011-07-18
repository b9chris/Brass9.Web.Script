using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script
{
	public abstract class ScriptResource
	{
		public string Name { get; set; }

		/// <summary>
		/// Temporary holding for names of scripts this script depends on
		/// </summary>
		public List<string> Dependencies { get; set; }

		/// <summary>
		/// Formal list of scripts this script depends on - built once after
		/// entire Dependency tree has been built (first Render)
		/// </summary>
		public ScriptResource[] ParentScripts
		{
			get
			{
				//if (parentScripts == null)
				//	BuildParentScripts();

				return parentScripts;
			}
		}
		protected ScriptResource[] parentScripts;


		public ScriptResource(string name, string[] dependencyNames)
		{
			Name = name;

			if (dependencyNames == null)
				Dependencies = new List<string>();
			else
				Dependencies = new List<string>(dependencyNames);
		}


		public void BuildParentScripts()
		{
			var names = Dependencies;
			parentScripts = new ScriptResource[names.Count];

			// TODO: Use PageScripts, not AppScripts - initialize
			// PageScripts to be AppScripts, then add onto it
			// Either that or search both here
			// IncludeScript calls on-page aren't being allowed as dependencies right now
			for (int i = names.Count - 1; i >= 0; i--)
			{
				string scriptNameToLookup = names[i];
				if (!AppScripts.Current.List.ContainsKey(scriptNameToLookup))
					throw new ScriptNotRegisteredException("Page tried to render script \"" + scriptNameToLookup + "\" without defining it.");

				var parentScript = AppScripts.Current.List[scriptNameToLookup];
				parentScripts[i] = parentScript;
				if (parentScript.ParentScripts == null)
					parentScript.BuildParentScripts();
			}

			//Dependencies = null;
		}
	}
}
