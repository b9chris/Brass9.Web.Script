using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script
{
	public class PageScriptMapper
	{
		// We'll map ScriptResource objects from PageScripts into this Dictionary
		// as we process them for dependencies. There will be more than just the
		// ones in PageScripts, since PageScripts is just the leaves of the
		// dependency tree. There could be one PageScript and 24 parents, or
		// 24 PageScripts with 1 parent - we have no idea how big this Dictionary
		// is going to be yet.
		public Dictionary<ScriptResource, ScriptLoadItem> ProcessedScripts = new Dictionary<ScriptResource, ScriptLoadItem>();

		protected HashSet<ScriptLoadGroup> root = new HashSet<ScriptLoadGroup>();

		protected ScriptLoadGroup simpleScriptGroup = new ScriptLoadGroup();


		public HashSet<ScriptLoadGroup> MapScripts(HashSet<ScriptResource> pageScripts)
		{
			// TODO: A setup like this:
			// jquery -> site -> loggedin -> shipmenttotals -> dash
			// jquery -> site -> rsswidget
			// Causes this to lose scripts - shipmenttotals and dash are thrown out

			// TODO: A setup like this:
			// jquery -> site -> loggedin -> shipmenttotals -> dash
			// jquery, excanvas -> flot -> dash
			// Causes an infinite loop - presumably from jquery being too deep to properly merge


			// Just stupidly map it all out every time - in the future we should
			// use caching and better data structures to make this smart

			// The global list of ScriptResource objects declared in Global.asax
			Dictionary<string, ScriptResource> appScripts = AppScripts.Current.List;

			foreach (ScriptResource resource in pageScripts)
			{
				if (ProcessedScripts.ContainsKey(resource))
				{
					// This script has already been put into a group, so we
					// can just skip it.
					continue;
				}

				ScriptLoadItem script = processScript(resource);

				if (script.Script.ParentScripts.Length == 0)
				{
					simpleScriptGroup.AddScript(script);
				}
				else
				{
					// Look for its dependency already added
					// If it's not there, add the dependency to a new root ScriptGroup,
					// then add this as its only child
					// If it is there, add this to the list of children for that ScriptGroup
					// If there are more than one dependency, find all dependencies,
					// make new ones for ones that don't exist yet, and merge them all together

					ScriptLoadGroup group = new ScriptLoadGroup();
					group.AddScript(script);

					ScriptLoadGroup parentGroup = new ScriptLoadGroup();
					group.Parent = parentGroup;

					parentGroup = buildParentScriptGroup(script.Script.ParentScripts, parentGroup);

					// Find the highest level parent, and add it to root
					while (parentGroup.Parent != null)
						parentGroup = parentGroup.Parent;

					root.Add(parentGroup);
				}
			}

			if (simpleScriptGroup.Scripts.Count > 0)
				root.Add(simpleScriptGroup);

			return root;
		}

		protected ScriptLoadItem processScript(ScriptResource resource)
		{
			// TODO: This prevents us from referencing scripts on-page because these parent script trees
			// are built once for the app for all AppScripts, then repeatedly and lost on each request for all
			// PageScripts
			if (resource.ParentScripts == null)
				resource.BuildParentScripts();
			ScriptLoadItem script = new ScriptLoadItem(resource);
			ProcessedScripts[resource] = script;
			return script;
		}

		/// <summary>
		/// Recursively inject parents up the tree of dependencies
		/// Eventually we don't have any dependencies (parents), so the
		/// ultimate parent group has its Parent set to null, meaning it
		/// sits at the root
		/// </summary>
		/// <param name="scripts"></param>
		/// <returns></returns>
		protected ScriptLoadGroup buildParentScriptGroup(IEnumerable<ScriptResource> scripts, ScriptLoadGroup mergeGroup)
		{
			ScriptLoadGroup group;
			// If no group to merge into was passed, we're building a new one
			if (mergeGroup == null)
				group = new ScriptLoadGroup();
			else
				group = mergeGroup; 

			List<ScriptResource> parents = new List<ScriptResource>();

			foreach (ScriptResource script in scripts)
			{
				if (ProcessedScripts.ContainsKey(script))
				{
					// Already processed.
					ScriptLoadItem existingScriptItem = ProcessedScripts[script];
					ScriptLoadGroup existingGroup = existingScriptItem.Group;

					if (existingGroup == simpleScriptGroup)
					{
						// The script is in the simpleScriptGroup, which we want to
						// keep simple. Move this script out of it and into our new
						// group.
						existingGroup.MoveScript(group, existingScriptItem);
					}
					else if (existingGroup != group)
					{
						// We need to merge our current group with the
						// group this script already lives in.
						// But, if the group this script lives in actually has this script as a
						// dependency, we're going to cause an infinite loop by merging. Check the
						// members for dependencies that include this one. If there are, remove it
						// from the existingGroup. The group we built above will now simply be the
						// remaining scripts' parent.

						ScriptLoadGroup siblingDependentGroup = null;
						ScriptLoadGroup checkGroup = existingGroup;
						while (checkGroup != null)
						{
							foreach (ScriptLoadItem sibling in checkGroup.Scripts)
							{
								if (sibling.Script.ParentScripts.Contains(script))
								{
									siblingDependentGroup = checkGroup;
									break;
								}
							}
							checkGroup = checkGroup.Parent;
						}

						if (siblingDependentGroup != null)
						{
							existingGroup.Scripts.Remove(existingScriptItem);
							group.AddScript(existingScriptItem);
						}
						else
						{
							existingGroup.Merge(group);
							group = existingGroup;
						}
					}
					// else
					// {
						// No work needed. This script has already been placed in this
						// group (the group passed in as mergeGroup above).
					// }
				}
				else
				{
					// Ensure this is in the map for looking up during dependency searches
					ScriptLoadItem loadItem = processScript(script);

					// Dump the script into the group
					group.AddScript(loadItem);
				}

				// Build a parent list for all new scripts in this group
				foreach (ScriptResource resource in script.ParentScripts)
					parents.Add(resource);
			}

			// If there were any new parents, we need to recurse to build a
			// parent group
			if (parents.Count > 0)
			{
				// If the mergeGroup had a null parent, it was passed in already added
				// to the root; in this case we need to pull it from the root, as we're
				// about to make it a child of a new parent group.
				if (mergeGroup != null && mergeGroup.Parent == null)
					root.Remove(mergeGroup);

				if (group.Parent == null)	// Build a new parent
					group.Parent = buildParentScriptGroup(parents);
				else	// Merge parents into the passed-in group's existing parent
					buildParentScriptGroup(parents, group.Parent);
			}

			return group;
		}

		protected ScriptLoadGroup buildParentScriptGroup(IEnumerable<ScriptResource> scripts)
		{
			return buildParentScriptGroup(scripts, null);
		}
	}
}
