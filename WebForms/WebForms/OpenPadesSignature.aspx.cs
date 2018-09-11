using Lacuna.Pki.Pades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {
	public partial class OpenPadesSignature : System.Web.UI.Page {

		// Properties used by the aspx to render the page (we'll fill these values on Page_Load).
		protected PadesSignatureModel Model { get; set; }

		protected void Page_Load(object sender, EventArgs e) {

			// Get userfile from query string.
			var userfile = Request.QueryString["userfile"];

			// Our action only works if a userfile is given to work with.
			if (string.IsNullOrEmpty(userfile)) {
				// Return "Not Found" HTTP response.
				Response.StatusCode = 404;
				Response.End();
				return;
			}

			// Read document from storage.
			var fileContent = Storage.GetFile(userfile);

			// Open an validate signatures with PKI SDK based on the PAdES Basic policy.
			var signature = Lacuna.Pki.Pades.PadesSignature.Open(fileContent);
			var policyMapper = PadesPoliciesForGeneration.GetPadesBasic(Util.GetTrustArbitrator());

			// Generate a model to be shown on the page from the PadesSignature instance computed from Open()
			// method above. This class can be inspected on SignatureModels.cs file. In this class, we validate
			// each signature based on the policy mapper defined above.
			var model = new PadesSignatureModel(signature, policyMapper);

			// Set property for rendering on page (see aspx file).
			this.Model = model;
		}
	}
}