using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using Brass9.Web.Style;
using Brass9.Web.Script;


namespace Brass9WebScriptExample
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : System.Web.HttpApplication
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
				new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
			);

		}

		protected void registerStyles(AppCss styles)
		{
			styles.CssFolder = "/ui/";

			styles.Add("component1", "component1.css", "component1.css");
		}

		protected void registerScripts(AppScripts scripts)
		{
			scripts.ScriptsFolder = "/ui/";

			scripts.Add("component1", "component1.js", "component1.js");
			scripts.Add("component2", "component2.js", "component2.js", "component1");
			scripts.Add("pagescript1", "pagescript1.js", "pagescript1.js", "component2");
		}

		protected void Application_Start()
		{
			// Get common style and scripts ready
			registerStyles(AppCss.Current);
			registerScripts(AppScripts.Current);

			AreaRegistration.RegisterAllAreas();

			RegisterGlobalFilters(GlobalFilters.Filters);
			RegisterRoutes(RouteTable.Routes);
		}
	}
}