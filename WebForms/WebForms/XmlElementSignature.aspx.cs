using Lacuna.Pki;
using Lacuna.Pki.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {
	public partial class XmlElementSignature : System.Web.UI.Page {

		public string SignatureFile { get; private set; }
		public PKCertificate Certificate { get; private set; }

		protected void Page_Load(object sender, EventArgs e) {

		}

		protected void SubmitCertificateButton_Click(object sender, EventArgs e) {

			byte[] toSignHash, transferData;
			SignatureAlgorithm signatureAlg;

			try {
				// Instantiate a CadesSigner class
				var signer = new XmlElementSigner();

				// Set the data to sign, which in the case of this example is a fixed sample document
				signer.SetXml(Storage.GetSampleNFeContent());

				// static Id from node <infNFe> from SampleNFe.xml document
				signer.SetToSignElementId("NFe35141214314050000662550010001084271182362300");

				// Decode the user's certificate and set as the signer certificate
				signer.SetSigningCertificate(PKCertificate.Decode(Convert.FromBase64String(CertContentField.Value)));

				// Set the signature policy
				signer.SetPolicy(getSignaturePolicy());

				// Generate the "to-sign-hash". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignHash = signer.GenerateToSignHash(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired. 
				ex.ValidationResults.Errors.ForEach(ve => ModelState.AddModelError("", ve.ToString()));
				return;
			}

			// On the next step (SubmitSignatureButton_Click action), we'll need once again some information:
			// - The "to-sign-hash"
			// - The "to-sign-bytes" passed by TransferData field
			// - The OID of the digest algorithm to be used during the signature operation
			// We'll set the hidden fields on this page, that'll be loaded again.
			ToSignHashField.Value = Convert.ToBase64String(toSignHash);
			TransferDataField.Value = Convert.ToBase64String(transferData);
			DigestAlgorithmField.Value = signatureAlg.DigestAlgorithm.Oid;

			// We'll hide the signatureControlPanel, because it's unnecessary on the next steps of the signature.
			signatureControlsPanel.Visible = false;
		}

		protected void SubmitSignatureButton_Click(object sender, EventArgs e) {

			byte[] signatureContent;

			try {

				var signer = new XmlElementSigner();

				// Set the document to be signed and the policy, exactly like in the previous action (SubmitCertificateButton_Click)
				signer.SetXml(Storage.GetSampleNFeContent());
				signer.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "to-sign-bytes" recovered from the page
				signer.SetPrecomputedSignature(Convert.FromBase64String(SignatureField.Value), Convert.FromBase64String(TransferDataField.Value));

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				signer.ComputeSignature();

				// Get the signed XML as an array of bytes
				signatureContent = signer.GetSignedXml();

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
			this.SignatureFile = Storage.StoreFile(signatureContent, ".xml");
			this.Certificate = PKCertificate.Decode(Convert.FromBase64String(CertContentField.Value));

			Server.Transfer("XmlElementSignatureInfo.aspx");
		}

		/**
			This method defines the signature policy that will be used on the signature.
		 */
		private XmlPolicySpec getSignaturePolicy() {

			var policy = BrazilXmlPolicySpec.GetNFePadraoNacional();

#if DEBUG
			// During debug only, we clear the policy's default trust arbitrator (which, in the case of
			// the policy returned by BrazilXmlPolicySpec.GetNFePadraoNacional(), corresponds to the ICP-Brasil roots only),
			// and use our custom trust arbitrator which accepts test certificates (see Util.GetTrustArbitrator())
			policy.ClearTrustArbitrators();
			policy.AddTrustArbitrator(Util.GetTrustArbitrator());
#endif

			return policy;
		}
	}
}