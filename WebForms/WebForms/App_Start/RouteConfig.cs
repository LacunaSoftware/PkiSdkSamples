using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.FriendlyUrls;
using System.Web.Http;

namespace WebForms {
	public static class RouteConfig {
		public static void RegisterRoutes(RouteCollection routes) {
			var settings = new FriendlyUrlSettings {
				AutoRedirectMode = RedirectMode.Permanent
			};
			routes.EnableFriendlyUrls(settings);
		}
	}
}
