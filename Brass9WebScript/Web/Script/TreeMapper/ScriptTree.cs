using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Brass9.Data.Linq;


namespace Brass9.Web.Script.TreeMapper
{
	public class ScriptTree
	{
		/// <summary>
		/// The descending layers of the tree
		/// </summary>
		public List<ScriptTreeLayer> Layers = new List<ScriptTreeLayer>();

		/// <summary>
		/// A map of script names to what layer each resides in
		/// </summary>
		public Dictionary<string, int> ScriptLayerMap = new Dictionary<string, int>();

		public ScriptTree()
		{
		}

		public void BuildDownFromTop(ScriptsBag scriptsBag, TreeSharedDependencyMap sharedDependencies)
		{
			// Loop through its dependencies, add to (create?) layer below
			int targetLayer = 0;

			var currentScript = Layers[0].Scripts.Values.First();

			var scriptsForLayer = new HashSet<string>(currentScript.Dependencies);
			bool layerAdded = true;

			for (targetLayer++; layerAdded; targetLayer++)
			{
				if (scriptsForLayer.Count == 0)
				{
					break;
				}

				// This layer may have already been added by PushDown; if not, add it
				EnsureLayer(targetLayer);

				foreach (string scriptName in scriptsForLayer)
				{
					// Has this script already been added to this tree?
					if (!ScriptLayerMap.ContainsKey(scriptName))
					{
						// No

						AddScriptToLayer(targetLayer, scriptName, scriptsBag, sharedDependencies);
						continue;
					}

					// Yes
					MaybePushDown(scriptName, targetLayer, scriptsBag, sharedDependencies);
				}

				if (!layerAdded)
					continue;

				scriptsForLayer = Layers[targetLayer].Scripts.Values.SelectMany(s => s.Dependencies).ToHashSet();
			}
		}

		public ScriptTreeLayer AddLayer(int layerNumber)
		{
			var layer = new ScriptTreeLayer(this, layerNumber);
			if (Layers.Count == layerNumber)
				Layers.Add(layer);
			else
				throw new Exception("Attempt to add layer " + layerNumber + " when layer count is " + Layers.Count);

			return layer;
		}

		public void AddScriptToLayer(int layerNumber, string scriptName, ScriptsBag scriptsBag, TreeSharedDependencyMap sharedDependencies)
		{
			ScriptLayerMap.Add(scriptName, layerNumber);
			Layers[layerNumber].AddScript(scriptName, scriptsBag[scriptName]);
			sharedDependencies.ScriptIsInTree(scriptName, this);
		}

		public void MoveScriptInTree(int targetLayer, string scriptName, ScriptsBag scriptsBag)
		{
			Layers[ScriptLayerMap[scriptName]].Scripts.Remove(scriptName);
			ScriptLayerMap[scriptName] = targetLayer;
			Layers[targetLayer].AddScript(scriptName, scriptsBag[scriptName]);
		}

		// Recursive
		public void MaybePushDown(string scriptName, int targetLayer, ScriptsBag scriptsBag, TreeSharedDependencyMap sharedDependencies)
		{
			//if (ScriptLayerMap[scriptName] == targetLayer)
			//{
			// We don't need to do anything, the script is already here - skip it
			//}
			// The dependency script may not have been added yet in a recursive PushDown, even if it would have been had we waited -
			// check and allow for it (it needs to be added instead of pushed down)
			if (!ScriptLayerMap.ContainsKey(scriptName) || ScriptLayerMap[scriptName] < targetLayer)
			{
				// The script is already added higher in the tree, preventing it from loading before it's needed by the script
				// calling this as a dependency. That means it needs to be pushed lower in the tree, to the targetLayer.
				PushDown(scriptName, targetLayer, scriptsBag, sharedDependencies);
			}
		}

		// Recursive
		public void PushDown(string scriptName, int targetLayer, ScriptsBag scriptsBag, TreeSharedDependencyMap sharedDependencies)
		{
			EnsureLayer(targetLayer);

			if (ScriptLayerMap.ContainsKey(scriptName))
				// Push script from current to target layer
				MoveScriptInTree(targetLayer, scriptName, scriptsBag);
			else
				AddScriptToLayer(targetLayer, scriptName, scriptsBag, sharedDependencies);

			// then loop through its dependencies
			// and proceed pushing them down recursively
			targetLayer++;
			var dependencies = scriptsBag[scriptName].Dependencies;
			foreach (string dependency in dependencies)
			{
				MaybePushDown(dependency, targetLayer, scriptsBag, sharedDependencies);
			}
		}

		public void EnsureLayer(int layerNumber)
		{
			if (Layers.Count <= layerNumber)
				AddLayer(layerNumber);
		}

		public int ScriptDistance(string script1, string script2)
		{
			return ScriptLayerMap[script1] - ScriptLayerMap[script2];
		}

		/// <summary>
		/// Inserts empty layers below layerNumber in the tree to accomodate an upcoming merge
		/// </summary>
		public void ExtendBelowLayer(int layerNumber, int numberOfLayersToInsert)
		{
			// Example: 0, 1, 2, 3
			// insert (a, b) below 1
			// goal: 0, 1, 2, 3, 4, 5
			// start at 2, add 2 and 3,
			// increase old 2 and 3 to 4 and 5, meaning after insert start at 4th index

			// These are Lists so this operation can be expensive
			layerNumber++;

			// Build the layer list
			var newLayers = new ScriptTreeLayer[numberOfLayersToInsert];
			for (int i = 0; i < numberOfLayersToInsert; i++)
				newLayers[i] = new ScriptTreeLayer(this, layerNumber + i);

			// Insert empty layers all at once to prevent repeated
			Layers.InsertRange(layerNumber, newLayers);

			// Fix layer numbers in the downstream
			for (int i = layerNumber + numberOfLayersToInsert; i < Layers.Count; i++)
				Layers[i].LayerNumber = i;
		}

		public int LayersBelowScript(string scriptName)
		{
			return Layers.Count - ScriptLayerMap[scriptName] - 1;
		}

		/// <summary>
		/// Returns the root of a ScriptLoadGroup chain, as the renderer expects
		/// </summary>
		public ScriptLoadGroup ToScriptLoadGroupChain()
		{
			ScriptLoadGroup group = null;
			foreach (var layer in Layers)
			{
				ScriptLoadGroup parentGroup = layer.ToScriptLoadGroup();
				if (group != null)
					group.Parent = parentGroup;
				group = parentGroup;
			}

			return group;
		}
	}
}
