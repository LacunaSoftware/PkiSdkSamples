using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebForms {

	public partial class CadesSignatureInfo : System.Web.UI.Page {

		protected string file { get; private set; }
		protected PKCertificate certificate { get; private set; }

		protected void Page_Load(object sender, EventArgs e) {
			if (PreviousPage == null) {
				Response.Redirect("~/");
				return;
			}

			if (!IsPostBack) {
				this.file = PreviousPage.File;
				this.certificate = PreviousPage.Certificate;
			}
		}
	}
}