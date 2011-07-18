using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script.TreeMapper
{
	public class ScriptsBag : Dictionary<string, ScriptResource>
	{
		public ScriptsBag()
			: base()
		{
		}

		public ScriptsBag(IDictionary<string, ScriptResource> dict)
			: base(dict)
		{
		}
	}
}
