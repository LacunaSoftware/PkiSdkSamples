using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.UI;

namespace WebForms {

	public class BundleConfig {

		// For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkID=303951
		public static void RegisterBundles(BundleCollection bundles) {

			bundles.Add(new ScriptBundle("~/bundles/WebFormsJs").Include(
				"~/Scripts/WebForms/WebForms.js",
				"~/Scripts/WebForms/WebUIValidation.js",
				"~/Scripts/WebForms/MenuStandards.js",
				"~/Scripts/WebForms/Focus.js",
				"~/Scripts/WebForms/GridView.js",
				"~/Scripts/WebForms/DetailsView.js",
				"~/Scripts/WebForms/TreeView.js",
				"~/Scripts/WebForms/WebParts.js"
			));

			// Order is very important for these files to work, they have explicit dependencies
			bundles.Add(new ScriptBundle("~/bundles/MsAjaxJs").Include(
				"~/Scripts/WebForms/MsAjax/MicrosoftAjax.js",
				"~/Scripts/WebForms/MsAjax/MicrosoftAjaxApplicationServices.js",
				"~/Scripts/WebForms/MsAjax/MicrosoftAjaxTimer.js",
				"~/Scripts/WebForms/MsAjax/MicrosoftAjaxWebForms.js"
			));

			// Use the Development version of Modernizr to develop with and learn from. Then, when you’re
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
				"~/Scripts/modernizr-*"
			));

			ScriptManager.ScriptResourceMapping.AddDefinition("respond", new ScriptResourceDefinition {
				Path = "~/Scripts/respond.min.js",
				DebugPath = "~/Scripts/respond.js",
			});

			ScriptManager.ScriptResourceMapping.AddDefinition("BlockUI", new ScriptResourceDefinition {
				Path = "~/Scripts/jquery.blockUI.js"
			});

			ScriptManager.ScriptResourceMapping.AddDefinition("WebPKI", new ScriptResourceDefinition {
				Path = "~/Scripts/lacuna-web-pki-2.9.0.js"
			});

			ScriptManager.ScriptResourceMapping.AddDefinition("AuthenticationForm", new ScriptResourceDefinition {
				Path = "~/Scripts/App/authentication-form.js"
			});

			ScriptManager.ScriptResourceMapping.AddDefinition("SignatureForm", new ScriptResourceDefinition {
				Path = "~/Scripts/App/signature-form.js"
			});

			ScriptManager.ScriptResourceMapping.AddDefinition("BatchSignatureForm", new ScriptResourceDefinition {
				Path = "~/Scripts/App/batch-signature-form.js"
			});
		}
	}
}
