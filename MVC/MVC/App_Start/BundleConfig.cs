using System.Web;
using System.Web.Optimization;

namespace MVC {
	public class BundleConfig {
		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles) {
			bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
							"~/Content/js/jquery-{version}.js",
							"~/Content/js/jquery.blockUI.js"));

			bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
							"~/Content/js/jquery.validate*"));

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
							"~/Content/js/modernizr-*"));

			bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
						 "~/Content/js/bootstrap.js",
						 "~/Content/js/respond.js"));

			bundles.Add(new StyleBundle("~/Content/css").Include(
						 "~/Content/css/bootstrap.css",
						 "~/Content/css/bootstrap-theme.css",
						 "~/Content/css/site.css"));
		}
	}
}
