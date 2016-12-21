using Lacuna.Pki;
using Lacuna.Pki.Cades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {
	public partial class CadesSignature : System.Web.UI.Page {

		public string SignatureFile { get; private set; }
		public PKCertificate Certificate { get; private set; }

		protected void Page_Load(object sender, EventArgs e) {

		}

		protected void SubmitCertificateButton_Click(object sender, EventArgs e) {

			byte[] toSignBytes;
			SignatureAlgorithm signatureAlg;

			try {
				// Instantiate a CadesSigner class
				var cadesSigner = new CadesSigner();

				// Set the data to sign, which in the case of this example is a fixed sample document
				cadesSigner.SetDataToSign(Storage.GetSampleDocContent());

				// Decode the user's certificate and set as the signer certificate
				cadesSigner.SetSigningCertificate(PKCertificate.Decode(Convert.FromBase64String(CertContentField.Value)));

				// Set the signature policy
				cadesSigner.SetPolicy(getSignaturePolicy());

				// Generate the "to-sign-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignBytes = cadesSigner.GenerateToSignBytes(out signatureAlg);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired. 
				ex.ValidationResults.Errors.ForEach(ve => ModelState.AddModelError("", ve.ToString()));
				return;
			}

			// On the next step (SubmitSignatureButton_Click action), we'll need once again some information:
			// - The "to-sign-bytes"
			// - The "to-sign-hash" (digest of the "to-sign-bytes")
			// - The OID of the digest algorithm to be used during the signature operation
			// We'll set the hidden fields on this page, that'll be loaded again.
			ToSignBytesField.Value = Convert.ToBase64String(toSignBytes);
			ToSignHashField.Value = Convert.ToBase64String(signatureAlg.DigestAlgorithm.ComputeHash(toSignBytes));
			DigestAlgorithmField.Value = signatureAlg.DigestAlgorithm.Oid;

			// We'll hide the signatureControlPanel, because it's unnecessary on the next steps of the signature.
			signatureControlsPanel.Visible = false;
		}

		protected void SubmitSignatureButton_Click(object sender, EventArgs e) {

			byte[] signatureContent;

			try {

				var cadesSigner = new CadesSigner();

				// Set the document to be signed and the policy, exactly like in the previous action (SubmitCertificateButton_Click)
				cadesSigner.SetDataToSign(Storage.GetSampleDocContent());
				cadesSigner.SetPolicy(getSignaturePolicy());

				// Set signer's certificate
				cadesSigner.SetSigningCertificate(PKCertificate.Decode(Convert.FromBase64String(CertContentField.Value)));

				// Set the signature computed on the client-side, along with the "to-sign-bytes" recovered from the page
				cadesSigner.SetPrecomputedSignature(Convert.FromBase64String(SignatureField.Value), Convert.FromBase64String(ToSignBytesField.Value));

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				cadesSigner.ComputeSignature();

				// Get the signature as an array of bytes
				signatureContent = cadesSigner.GetSignature();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				ex.ValidationResults.Errors.ForEach(ve => ModelState.AddModelError("", ve.ToString()));
				// Set hidden field to indicate in signature-forms.js that the signature failed here.
				FormIsValidField.Value = Convert.ToString(false);
				return;
			}

			// Pass the following fields to be used on CadesSignatureInfo page:
			// - The signature file will be stored on the folder "App_Data/". Its name will be passed by SignatureFile field.
			// - The user's certificate
			this.SignatureFile = Storage.StoreFile(signatureContent, ".p7s");
			this.Certificate = PKCertificate.Decode(Convert.FromBase64String(CertContentField.Value));

			Server.Transfer("CadesSignatureInfo.aspx");
		}

		/**
		 *	This method defines the signature policy that will be used on the signature.
		 */
		private ICadesPolicyMapper getSignaturePolicy() {

			var policy = CadesPoliciesForGeneration.GetPkiBrazilAdrBasica();

#if DEBUG
			// During debug only, we return a wrapper which will overwrite the policy's default trust arbitrator (which in this case
			// corresponds to the ICP-Brasil roots only), with our custom trust arbitrator which accepts test certificates
			// (see Util.GetTrustArbitrator())
			return new CadesPolicyMapperWrapper(policy, Util.GetTrustArbitrator());
#else
			return policy;
#endif
		}
	}
}