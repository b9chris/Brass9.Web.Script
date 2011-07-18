using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script
{
	public class ScriptLoadItem
	{
		public ScriptLoadGroup Group;
		public ScriptResource Script;

		public ScriptLoadItem(ScriptResource script)
		{
			Script = script;
		}
	}
}
