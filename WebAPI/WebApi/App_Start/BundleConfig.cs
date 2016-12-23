using System.Web;
using System.Web.Optimization;

namespace WebApi {

	public class BundleConfig {

		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles) {

			bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
				"~/Scripts/jquery-{version}.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/angular").Include(
				"~/Scripts/angular.js",
				"~/Scripts/angular-route.js",
				"~/Scripts/angular-ui/ui-bootstrap-tpls.js",
				"~/Scripts/angular-block-ui.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
				"~/Scripts/modernizr-*"
			));

			bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
				"~/Scripts/bootstrap.js",
				"~/Scripts/respond.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/webpki").Include(
				"~/Scripts/lacuna-web-pki-{version}.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/app")
				.Include("~/app/app.js")
				.IncludeDirectory("~/app/controllers", "*.js")
			);

			bundles.Add(new StyleBundle("~/Content/css").Include(
				"~/Content/bootstrap.css",
				"~/Content/angular-block-ui.css",
				"~/Content/site.css"
			));
		}
	}
}
