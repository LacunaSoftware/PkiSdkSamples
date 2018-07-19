using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebForms {
	public partial class PadesSignatureInfo : System.Web.UI.Page {

		protected string signatureFile { get; private set; }
		protected PKCertificate certificate { get; private set; }

		protected void Page_Load(object sender, EventArgs e) {
			if (PreviousPage == null) {
				Response.Redirect("~/");
				return;
			}

			if (!IsPostBack) {
				this.signatureFile = PreviousPage.SignatureFile;
				this.certificate = PreviousPage.Certificate;
			}
		}
	}
}
