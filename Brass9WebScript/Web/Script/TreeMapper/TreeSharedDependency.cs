using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script.TreeMapper
{
	public class TreeSharedDependency
	{
		/// <summary>
		/// Name of the script
		/// </summary>
		public string Name;

		/// <summary>
		/// Trees the script resides in
		/// </summary>
		public HashSet<ScriptTree> Trees = new HashSet<ScriptTree>();

		public TreeSharedDependency(string name, ScriptTree firstTree)
		{
			Name = name;
			Trees.Add(firstTree);
		}
	}
}
