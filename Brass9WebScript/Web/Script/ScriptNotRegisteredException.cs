using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brass9.Web.Script
{
	public class ScriptNotRegisteredException : Exception
	{
		public ScriptNotRegisteredException()
			: base("A script was requested on the page that was not registered before render.")
		{
		}

		public ScriptNotRegisteredException(string message)
			: base(message)
		{
		}

		public ScriptNotRegisteredException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
