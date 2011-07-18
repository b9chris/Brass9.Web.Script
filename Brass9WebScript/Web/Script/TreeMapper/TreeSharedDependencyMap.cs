using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script.TreeMapper
{
	/// <summary>
	/// A map of scripts to trees they're in use in. Scripts in multiple trees indicate a shared dependency that requires
	/// tree merging before render.
	/// </summary>
	public class TreeSharedDependencyMap : Dictionary<string, TreeSharedDependency>
	{
		public void ScriptIsInTree(string name, ScriptTree tree)
		{
			if (ContainsKey(name))
				this[name].Trees.Add(tree);
			else
				Add(name, new TreeSharedDependency(name, tree));
		}
	}
}
