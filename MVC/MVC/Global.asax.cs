using MVC.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace MVC {

	public class MvcApplication : System.Web.HttpApplication {

		protected void Application_Start() {

			AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);

			Lacuna.Pki.NLogConnector.NLogLogger.Configure();

			// --------------------------------------------------------------------------------------------------------------
			// If you need to set a proxy for outgoing connections, uncomment the lines below and set the appropriate values
			// --------------------------------------------------------------------------------------------------------------
			//System.Net.WebRequest.DefaultWebProxy = new System.Net.WebProxy("http://your.proxy.server:8080") {
			//	BypassProxyOnLocal = true,
			//	Credentials = new System.Net.NetworkCredential("user", "password") // or UseDefaultCredentials = true
			//};
		}
	}
}
