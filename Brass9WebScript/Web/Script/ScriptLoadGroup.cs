using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script
{
	/// <summary>
	/// Data structure that represents the tree of scripts and dependencies
	/// for a specific View, before Rendering
	/// 
	/// A ScriptLoadGroup is a group of scripts that:
	/// 1) Can be loaded at the same time
	/// 2) Share the same list of dependents
	/// 
	/// For example if a.js depends on jquery.js and b.js depends on date.js,
	/// then there would be 2 root ScriptLoadGroups:
	/// "jquery.js"
	///   "a.js"
	/// 
	/// and
	/// "date.js"
	///   "b.js"
	///   
	/// If a c.js depended on nothing, it would be in its own separate root
	/// ScriptLoadGroup
	/// 
	/// If c.js depended on BOTH date.js and jquery.js, they would now need to be
	/// merged into a single ScriptLoadGroup so the dependency chain isn't violated,
	/// resulting in a tree like this:
	/// 
	/// "jquery.js", "date.js"
	///   "a.js", "b.js", "c.js"
	///   
	/// Note that c.js's complex dependency impacts performance a bit, in that
	/// a.js and b.js may now load a little later to avoid making things ultra
	/// complex.
	/// </summary>
	public class ScriptLoadGroup
	{
		/// <summary>
		/// The parent of this group, if any
		/// </summary>
		public ScriptLoadGroup Parent
		{
			get
			{
				return parent;
			}
			set
			{
				parent = value;
				if (value != null)
				{
					if (parent.ChildGroup == null)
					{
						parent.ChildGroup = this;
					}
					else
					{
						// Merge existing ChildGroup and this one
						parent.ChildGroup.Merge(this);
					}
				}
			}
		}
		protected ScriptLoadGroup parent;

		/// <summary>
		/// The scripts this group actually contains
		/// </summary>
		public List<ScriptLoadItem> Scripts = new List<ScriptLoadItem>();

		/// <summary>
		/// The children of this group that depend on these scripts
		/// </summary>
		public ScriptLoadGroup ChildGroup;


		public ScriptLoadGroup(ScriptLoadGroup parent)
		{
			Parent = parent;
		}

		public ScriptLoadGroup()
		{
		}


		public void AddScript(ScriptLoadItem script)
		{
			Scripts.Add(script);
			script.Group = this;
		}

		/// <summary>
		/// Merge a ScriptLoadGroup into this group
		/// </summary>
		/// <param name="group">The group to merge into this one</param>
		public void Merge(ScriptLoadGroup group)
		{
			System.Diagnostics.Debug.Assert(this != group, "Can't merge ScriptLoadGroup into itself!");

			if (group.Parent != null && this.Parent != null && group.Parent != this.Parent)
			{
				// Both groups have parents, meaning the merge needs to
				// start there, not this low in the tree.
				this.Parent.Merge(group.Parent);
				return;
			}

			foreach (ScriptLoadItem script in group.Scripts)
			{
				this.AddScript(script);
			}

			if (group.ChildGroup != null)
			{
				if (ChildGroup == null)
					ChildGroup = group.ChildGroup;
				else	// Recurse down children if both groups have them
				{
					// Detach the child group before merging, or we'll trigger an infinite
					// loop where it comes back to this already merged parent group to try
					// to merge it
					group.ChildGroup.Parent = null;
					ChildGroup.Merge(group.ChildGroup);
				}
			}
		}

		public void MoveScript(ScriptLoadGroup to, ScriptLoadItem script)
		{
			Scripts.Remove(script);
			to.AddScript(script);
		}
	}
}
