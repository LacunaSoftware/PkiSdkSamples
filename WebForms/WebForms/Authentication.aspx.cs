using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {

	public partial class Authentication : System.Web.UI.Page {

		public PKCertificate AuthenticatedCertificate { get; private set; }
		public ValidationResults ValidationResults { get; private set; }

		protected void Page_Load(object sender, EventArgs e) {
			if (!IsPostBack) {
				startNewAuth();
			}
		}

		protected void SubmitButton_Click(object sender, EventArgs e) {

			var nonceStore = Util.GetNonceStore();
			var certAuth = new PKCertificateAuthentication(nonceStore);

			PKCertificate certificate;
			var vr = certAuth.Complete(Convert.FromBase64String(NonceField.Value), Convert.FromBase64String(CertificateField.Value), Convert.FromBase64String(SignatureField.Value), Util.GetTrustArbitrator(), out certificate);
			if (!vr.IsValid) {
				vr.Errors.ForEach(ve => ModelState.AddModelError("", ve.ToString()));
				startNewAuth();
				return;
			}

			this.AuthenticatedCertificate = certificate;
			this.ValidationResults = vr;
			Server.Transfer("AuthenticationSuccess.aspx");
		}

		private void startNewAuth() {
			var nonceStore = Util.GetNonceStore();
			var certAuth = new PKCertificateAuthentication(nonceStore);
			var nonce = certAuth.Start();
			NonceField.Value = Convert.ToBase64String(nonce);
			DigestAlgorithmField.Value = PKCertificateAuthentication.DigestAlgorithm.Oid;
		}
	}
}
