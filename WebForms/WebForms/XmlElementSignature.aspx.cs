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

				// Decode the user's certificate
				var cert = PKCertificate.Decode(Convert.FromBase64String(CertificateField.Value));

				// Instantiate a XmlElementSigner class
				var signer = new XmlElementSigner();

				// Set the data to sign, which in the case of this example is a fixed sample document
				signer.SetXml(Storage.GetSampleNFeContent());

				// static Id from node <infNFe> from SampleNFe.xml document
				signer.SetToSignElementId("NFe35141214314050000662550010001084271182362300");

				// Set as the signer certificate
				signer.SetSigningCertificate(cert);

				// Set the signature policy
				signer.SetPolicy(getSignaturePolicy());

				// Generate the "to-sign-hash". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy. as well as the "transfer data",
				// a byte-array that will be needed on the next step.
				toSignHash = signer.GenerateToSignHash(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired. 
				ex.ValidationResults.Errors.ForEach(ve => ModelState.AddModelError("", ve.ToString()));
				return;
			}

			// The "transfer data" for Xml signatures are not so big. Therefore, we can easily store it in a hidden 
			// field.
			TransferDataField.Value = Convert.ToBase64String(transferData);

			// Send to the javascript the "to sign hash" of the document and the digest algorithm that must
			// be used on the signature algorithm computation
			ToSignHashField.Value = Convert.ToBase64String(toSignHash);
			DigestAlgorithmField.Value = signatureAlg.DigestAlgorithm.Oid;
		}

		protected void SubmitSignatureButton_Click(object sender, EventArgs e) {

			byte[] signatureContent;

			try {

				// Instantiate a XmlElementSigner class
				var signer = new XmlElementSigner();

				// Set the document to be signed and the policy, exactly like in the previous event (SubmitCertificateButton_Click)
				signer.SetXml(Storage.GetSampleNFeContent());
				signer.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data"
				signer.SetPrecomputedSignature(Convert.FromBase64String(SignatureField.Value), Convert.FromBase64String(TransferDataField.Value));

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				signer.ComputeSignature();

				// Get the signed XML as an array of bytes
				signatureContent = signer.GetSignedXml();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				ex.ValidationResults.Errors.ForEach(ve => ModelState.AddModelError("", ve.ToString()));
				CertificateField.Value = "";
				ToSignHashField.Value = "";
				return;
			}

			// Pass the following fields to be used on XmlElementSignatureInfo page:
			// - The signature file will be stored on the folder "App_Data/". Its name will be passed by SignatureFile field.
			// - The user's certificate
			this.SignatureFile = Storage.StoreFile(signatureContent, ".xml");
			this.Certificate = PKCertificate.Decode(Convert.FromBase64String(CertificateField.Value));

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