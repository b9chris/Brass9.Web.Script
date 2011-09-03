using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Brass9.Data.Linq;


namespace Brass9.Web.Script.TreeMapper
{
	public class PageScriptTreeMapper
	{
		/// <summary>
		/// All scripts in the map.
		/// </summary>
		public ScriptsBag ScriptsBag;// = new Dictionary<string, ScriptResource>();

		/// <summary>
		/// The trees of scripts to ultimately be rendered. End up getting converted to ScriptLoadGroups at the last minute.
		/// </summary>
		public HashSet<ScriptTree> Trees = new HashSet<ScriptTree>();

		public TreeSharedDependencyMap SharedDependencies = new TreeSharedDependencyMap();


		// TODO: Support multiple tree tops for one base. Requires changes to Renderer.
		public PageScriptTreeMapper()
		{
		}

		public HashSet<ScriptLoadGroup> MapScripts(HashSet<ScriptResource> pageScripts, Dictionary<string, ScriptResource> appScripts)
		{
			ScriptsBag = new ScriptsBag(appScripts);

			// First, start the trees, and map the page scripts into the source
			startTrees(pageScripts);

			// Now build trees downwards in isolation
			buildTreesDownward();

			consolidateSharedDependencies();

			// Merge trees where we find shared dependencies
			while (mergeTrees())
			{// mergeTrees does the work and returns false when it's done
			}

			var groupChainRootsFromTrees = new HashSet<ScriptLoadGroup>();
			foreach(var tree in Trees)
				groupChainRootsFromTrees.Add(tree.ToScriptLoadGroupChain());

			return groupChainRootsFromTrees;
		}


		public static string ScriptResourceKey(ScriptResource script)
		{
			if (String.IsNullOrEmpty(script.Name))
			{
				if (script is FileScriptResource)
					return (script as FileScriptResource).DebugPath;

				string body = (script as InlineScriptResource).Body;
				if (body.Length > 20)
					return body.Substring(0, 20);

				return body;
			}

			return script.Name;
		}

		protected void startTrees(HashSet<ScriptResource> pageScripts)
		{
			foreach (var script in pageScripts)
			{
				string name = ScriptResourceKey(script);

				// Support for pagescript dependencies!
				if (!ScriptsBag.ContainsKey(name))
					ScriptsBag.Add(name, script);
				else if (script.Dependencies.Count > 0)
					// PageScript was declared with dependencies - override the AppScripts version
					ScriptsBag[name] = script;

				var tree = new ScriptTree();
				var layer0 = tree.AddLayer(0);
				tree.AddScriptToLayer(0, name, ScriptsBag, SharedDependencies);
				Trees.Add(tree);
			}
		}

		protected void buildTreesDownward()
		{
			foreach (var tree in Trees)
			{
				tree.BuildDownFromTop(ScriptsBag, SharedDependencies);
			}
		}

		protected void consolidateSharedDependencies()
		{
			var singleUseScripts = SharedDependencies.Values.Where(st => st.Trees.Count == 1).Select(st => st.Name).ToArray();
			foreach (var scriptName in singleUseScripts)
				SharedDependencies.Remove(scriptName);
		}

		protected bool mergeTrees()
		{
			// Get the name of a script shared between 2 trees
			var sharedDependency = SharedDependencies.Values.FirstOrDefault();
			if (sharedDependency == null)	// There aren't any - we're done.
				return false;

			// Take the first 2 trees for that shared dependency
			var treesToMerge = sharedDependency.Trees.Take(2).ToHashSet();

			// List all scripts involved with those 2 trees
			var scriptsToMerge = SharedDependencies.Values.Where(st => st.Trees.IsSupersetOf(treesToMerge)).Select(st => st.Name);

			// Produce 2 sorted lists of these scripts in each tree, ordered by layerNumber
			var treesEnumerator = treesToMerge.GetEnumerator();
			treesEnumerator.MoveNext();
			var tree1 = treesEnumerator.Current;
			treesEnumerator.MoveNext();
			var tree2 = treesEnumerator.Current;

			// Tree1 and 2 now need to match in structure relative to these scripts in order to be easily merged.
			// We need to Extend each tree wherever one is shorter in distance between shared scripts than the other
			var tree1Order = getScriptsFlatOrderForTree(scriptsToMerge, tree1);

			/*
#if DEBUG
			var tree2Order = getScriptsFlatOrderForTree(scriptsToMerge, tree2);
			// TODO: This isn't quite true. Since the compare is arbitrary for scripts on the same level, we could end up throwing here
			// for:
			// a -> jqueryui, site -> jquery
			// We should really also be able to support this:
			// a -> jqueryui, something -> site -> jquery
			// This requires further PushDown calls on the trees to separate the 2 dependencies from their shared layer in tree1
			// Write this when needed
			if (!scriptOrdersMatch(scriptsToMerge, tree1, tree2))
				throw new Exception("ScriptTree dependencies are not in the same order as each other; we don't know how to merge these");
#endif
			*/

			for (int iTree1 = tree1Order.Length - 2; iTree1 >= 0; iTree1--)
			{
				string script1 = tree1Order[iTree1];
				string script2 = tree1Order[iTree1 + 1];
				var tree1Distance = tree1.ScriptDistance(script2, script1);
				var tree2Distance = tree2.ScriptDistance(script2, script1);

				if (tree1Distance == tree2Distance)	// They match in distance, leave it be
					continue;

				if (tree1Distance > tree2Distance)	// tree2 needs to be extended.
				{
					tree2.ExtendBelowLayer(tree2.ScriptLayerMap[script1], tree1Distance - tree2Distance);
					continue;
				}

				// tree1 needs to be extended
				tree1.ExtendBelowLayer(tree1.ScriptLayerMap[script1], tree2Distance - tree1Distance);
			}

			// Now finally, perform the actual merge between the trees.

			// TODO: Perform a less naive merge, where we end up with a common base where the first shared dependency occurs,
			// then several smaller trees sticking out the top
			// Decide which is longer, which will become the consumer. The other gets consumed.
			ScriptTree longer;
			ScriptTree shorter;
			if (tree1.Layers.Count >= tree2.Layers.Count)
			{
				longer = tree1;
				shorter = tree2;
			}
			else
			{
				longer = tree2;
				shorter = tree1;
			}

			// Ensure the tail end of longer is as long as the tail end of shorter
			string lastScriptName = tree1Order[tree1Order.Length - 1];
			int shorterEndLongerBy = shorter.LayersBelowScript(lastScriptName) - longer.LayersBelowScript(lastScriptName);
			if (shorterEndLongerBy > 0)
				longer.ExtendBelowLayer(longer.Layers.Count - 1, shorterEndLongerBy);

			// The actual merge
			for (int i = longer.Layers.Count - 1, j = shorter.Layers.Count - 1; i >= 0 && j >= 0; i--, j--)
			{
				foreach(var kv in shorter.Layers[j].Scripts)
				{
					if (!longer.Layers[i].Scripts.ContainsKey(kv.Key))
						longer.Layers[i].AddScript(kv.Key, kv.Value);
				}
			}

			// We eliminated a tree - eliminate it from the SharedDependencies list
			Trees.Remove(shorter);
			foreach (string scriptName in tree1Order)
			{
				if (SharedDependencies[scriptName].Trees.Count == 2)
					SharedDependencies.Remove(scriptName);
				else
					SharedDependencies[scriptName].Trees.Remove(shorter);
			}

			return true;
		}

		protected string[] getScriptsFlatOrderForTree(IEnumerable<string> scripts, ScriptTree tree)
		{
			return scripts.OrderBy(s => tree.ScriptLayerMap[s]).ToArray();
		}

		protected List<HashSet<string>> getScriptsOrderForTree(IEnumerable<string> scripts, ScriptTree tree)
		{
			string[] scriptsInOrder = getScriptsFlatOrderForTree(scripts, tree);

			var list = new List<HashSet<string>>();
			HashSet<string> current = null;

			string lastScript = null;
			foreach (string script in scriptsInOrder)
			{
				if (lastScript != null && tree.ScriptLayerMap[lastScript] == tree.ScriptLayerMap[script])
				{
					current.Add(script);
				}
				else
				{
					current = new HashSet<string>();
					list.Add(current);
					current.Add(script);
				}
				lastScript = script;
			}

			return list;
		}

		protected bool scriptOrdersMatch(IEnumerable<string> scripts, ScriptTree tree1, ScriptTree tree2)
		{
			var tree1Order = getScriptsOrderForTree(scripts, tree1);

			int lastLevel = -1;
			foreach (var hashSet in tree1Order)
			{
				int currentLevel = -1;
				foreach (string script in hashSet)
				{
					if (currentLevel == -1)
					{
						currentLevel = tree2.ScriptLayerMap[script];
						if (currentLevel <= lastLevel)
							return false;

						lastLevel = currentLevel;
					}
					else
					{
						if (tree2.ScriptLayerMap[script] != currentLevel)
							return false;
					}
				}
			}

			return true;
		}
	}
}
