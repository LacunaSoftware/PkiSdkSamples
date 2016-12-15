using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebForms {

	public partial class AuthenticationSuccess : System.Web.UI.Page {

		protected PKCertificate certificate;
		protected ValidationResults validationResults;

		protected void Page_Load(object sender, EventArgs e) {

			if (PreviousPage == null) {
				Response.Redirect("~/");
				return;
			}

			this.certificate = PreviousPage.AuthenticatedCertificate;
			this.validationResults = PreviousPage.ValidationResults;
		}
	}
}