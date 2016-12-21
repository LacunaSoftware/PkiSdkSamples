using Lacuna.Pki;
using Lacuna.Pki.Pades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {
	public partial class PadesSignature : System.Web.UI.Page {

		public string SignatureFile { get; private set; }
		public PKCertificate Certificate { get; private set; }
		
		protected void Page_Load(object sender, EventArgs e) {

		}

		protected void SubmitCertificateButton_Click(object sender, EventArgs e) {

			byte[] toSignBytes, transferData;
			SignatureAlgorithm signatureAlg;

			try {

				// Decode the user's certificate
				var cert = PKCertificate.Decode(Convert.FromBase64String(CertContentField.Value));

				// Instantiate a PadesSigner class
				var padesSigner = new PadesSigner();

				// Set the PDF to sign, which in the case of this example is a fixed sample document
				padesSigner.SetPdfToSign(Storage.GetSampleDocContent());

				// Set the signer certificate
				padesSigner.SetSigningCertificate(cert);

				// Set the signature policy
				padesSigner.SetPolicy(getSignaturePolicy());

				// Set the signature's visual representation options (this is optional). For more information, see
				// http://pki.lacunasoftware.com/Help/html/98095ec7-2742-4d1f-9709-681c684eb13b.htm
				var visual = new PadesVisualRepresentation2() {

					// Text of the visual representation
					Text = new PadesVisualText() {

						// Compose the message
						CustomText = String.Format("Assinado digitalmente por {0}", cert.SubjectDisplayName),

						// Specify that the signing time should also be rendered
						IncludeSigningTime = true,

						// Optionally set the horizontal alignment of the text ('Left' or 'Right'), if not set the default is Left
						HorizontalAlign = PadesTextHorizontalAlign.Left
					},
					// Background image of the visual representation
					Image = new PadesVisualImage() {

						// We'll use the image in Content/PdfStamp.png
						Content = Storage.GetPdfStampContent(),

						// Opacity is an integer from 0 to 100 (0 is completely transparent, 100 is completely opaque).
						Opacity = 70,

						// Align the image to the right
						HorizontalAlign = PadesHorizontalAlign.Right
					},
					// Set the position of the visual representation
					Position = PadesVisualAutoPositioning.GetFootnote()
				};
				padesSigner.SetVisualRepresentation(visual);

				// Generate the "to-sign-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy, as well as the "transfer data",
				// a byte-array that will be needed on the next step.
				toSignBytes = padesSigner.GetToSignBytes(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired. 
				ex.ValidationResults.Errors.ForEach(ve => ModelState.AddModelError("", ve.ToString()));
				return;
			}

			// On the next step (SubmitSignatureButton_Click action), we'll need once again some information:
			// - The "to-sign-hash" (digest of the "to-sign-bytes")
			// - The "transfer data"
			// - The OID of the digest algorithm to be used during the signature operation
			// We'll set the hidden fields on this page, that'll be loaded again.
			ToSignHashField.Value = Convert.ToBase64String(signatureAlg.DigestAlgorithm.ComputeHash(toSignBytes));
			TransferDataField.Value = Convert.ToBase64String(transferData);
			DigestAlgorithmField.Value = signatureAlg.DigestAlgorithm.Oid;

			// We'll hide the signatureControlPanel, because it's unnecessary on the next steps of the signature.
			signatureControlsPanel.Visible = false;
		}

		protected void SubmitSignatureButton_Click(object sender, EventArgs e) {

			byte[] signatureContent;

			try {

				var padesSigner = new PadesSigner();

				// Set the signature policy, exactly like in the Start method
				padesSigner.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data" recovered from the page
				padesSigner.SetPreComputedSignature(Convert.FromBase64String(SignatureField.Value), Convert.FromBase64String(TransferDataField.Value));

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				padesSigner.ComputeSignature();

				// Get the signed PDF as an array of bytes
				signatureContent = padesSigner.GetPadesSignature();

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
			this.SignatureFile = Storage.StoreFile(signatureContent, ".pdf");
			this.Certificate = PKCertificate.Decode(Convert.FromBase64String(CertContentField.Value));

			Server.Transfer("PadesSignatureInfo.aspx");
		}

		/**
		 *	This method defines the signature policy that will be used on the signature.
		 */
		private IPadesPolicyMapper getSignaturePolicy() {

			var policy = PadesPoliciesForGeneration.GetPkiBrazilAdrBasica();

#if DEBUG
			// During debug only, we return a wrapper which will overwrite the policy's default trust arbitrator (which in this case
			// corresponds to the ICP-Brasil roots only), with our custom trust arbitrator which accepts test certificates
			// (see Util.GetTrustArbitrator())
			return new PadesPolicyMapperWrapper(policy, Util.GetTrustArbitrator());
#else
			return policy;
#endif
		}
	}
}