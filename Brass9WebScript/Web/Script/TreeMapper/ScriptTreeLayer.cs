using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script.TreeMapper
{
	/// <summary>
	/// A single layer in a ScriptTree. 0 is top of tree, and higher LayerNumbers are beneath.
	/// Keeps track of its current layer and what scripts are in it, nothing else.
	/// </summary>
	public class ScriptTreeLayer
	{
		public ScriptTree Tree;

		/// <summary>
		/// A number between 0 and infinity indicating the layer of the tree this represents. There's only one layer for each number.
		/// </summary>
		public int LayerNumber;

		/// <summary>
		/// The scripts on this layer
		/// </summary>
		public Dictionary<string, ScriptResource> Scripts = new Dictionary<string, ScriptResource>();

		public ScriptTreeLayer(ScriptTree tree, int layerNumber)
		{
			Tree = tree;
			LayerNumber = layerNumber;
		}

		/// <summary>
		/// Adds script to layer
		/// </summary>
		public void AddScript(string scriptName, ScriptResource script)
		{
			Scripts.Add(scriptName, script);
		}

		public ScriptLoadGroup ToScriptLoadGroup()
		{
			var group = new ScriptLoadGroup();

			foreach (var script in Scripts.Values)
			{
				group.AddScript(new ScriptLoadItem(script));
			}

			return group;
		}
	}
}
