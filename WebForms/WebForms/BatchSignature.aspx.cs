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

	public partial class BatchSignature : System.Web.UI.Page {

		class DocumentItem {
			public int Id { get; set; }
			public string Error { get; set; }
			public string DownloadLink { get; set; }
		}

		private List<int> _documentIds;
		protected List<int> DocumentIds {
			get {
				if (_documentIds == null) {
					_documentIds = DocumentIdsField.Value.Split(',').Select(i => int.Parse(i)).ToList();
				}
				return _documentIds;
			}
			set {
				DocumentIdsField.Value = string.Join(",", value);
				_documentIds = value;
			}
		}

		private int _documentIndex;
		protected int DocumentIndex {
			get {
				int index;
				if (!int.TryParse(DocumentIndexField.Value, out index)) {
					index = -1;
				}
				return index;
			}
			set {
				DocumentIndexField.Value = value.ToString();
				_documentIndex = value;
			}
		}

		protected void Page_Load(object sender, EventArgs e) {
			if (!IsPostBack) {
				DocumentIds = Enumerable.Range(1, 10).ToList();
				DocumentsListView.DataSource = DocumentIds.ConvertAll(i => new DocumentItem() { Id = i });
				DocumentsListView.DataBind();
			}
		}

		protected void SubmitCertificateButton_Click(object sender, EventArgs e) {
			DocumentIndex = -1;
			SignatureControlsPanel.Visible = false;
			startNextSignature();
		}

		protected void SubmitSignatureButton_Click(object sender, EventArgs e) {
			completeSignature();
		}

		private void startNextSignature() {

			DocumentIndex += 1;
			if (DocumentIndex == DocumentIds.Count) {
				ToSignHashField.Value = "(end)";
				return;
			}

			var docId = DocumentIds[DocumentIndex];

			byte[] toSignBytes, transferData;
			SignatureAlgorithm signatureAlg;

			try {

				// Decode the user's certificate
				var cert = PKCertificate.Decode(Convert.FromBase64String(CertContentField.Value));

				// Instantiate a PadesSigner class
				var padesSigner = new PadesSigner();

				// Set the PDF to sign, which in the case of this example is a fixed sample document
				padesSigner.SetPdfToSign(Storage.GetBatchDocContent(docId));

				// Set the signer certificate
				padesSigner.SetSigningCertificate(cert);

				// Set the signature policy
				padesSigner.SetPolicy(getSignaturePolicy());

				// Set the signature's visual representation options (this is optional). For more information, see
				// http://pki.lacunasoftware.com/Help/html/98095ec7-2742-4d1f-9709-681c684eb13b.htm
				var visual = new PadesVisualRepresentation2() {

					// Text of the visual representation
					Text = new PadesVisualText() {

						// used to compose the message
						CustomText = String.Format("Assinado digitalmente por {0}", cert.SubjectDisplayName),

						// Specify that the signing time should also be rendered
						IncludeSigningTime = true,

						// Optionally set the horizontal alignment of the text ('Left' or 'Right'), if not set the default is Left
						HorizontalAlign = PadesTextHorizontalAlign.Left
					},
					// Background image of the visual representation
					Image = new PadesVisualImage() {

						// We'll use as background the image in Content/PdfStamp.png
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

				setValidationError(ex.ValidationResults);
				startNextSignature();
				return;

			} catch (Exception ex) {

				setError(ex.Message);
				startNextSignature();
				return;

			}

			Session["TransferData"] = transferData;
			ToSignHashField.Value = Convert.ToBase64String(signatureAlg.DigestAlgorithm.ComputeHash(toSignBytes));
			DigestAlgorithmField.Value = signatureAlg.DigestAlgorithm.Oid;
		}

		private void completeSignature() {

			byte[] signatureContent;

			try {

				var padesSigner = new PadesSigner();

				// Set the signature policy, exactly like in the Start method
				padesSigner.SetPolicy(getSignaturePolicy());

				var signature = Convert.FromBase64String(SignatureField.Value);
				if (DocumentIndex == 4) {
					signature[0] = 0;
					signature[1] = 0;
					signature[2] = 0;
				}

				// Set the signature computed on the client-side, along with the "transfer data" recovered from the database
				padesSigner.SetPreComputedSignature(signature, (byte[])Session["TransferData"]);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				padesSigner.ComputeSignature();

				// Get the signed PDF as an array of bytes
				signatureContent = padesSigner.GetPadesSignature();

			} catch (ValidationException ex) {

				setValidationError(ex.ValidationResults);
				startNextSignature();
				return;

			} catch (Exception ex) {

				setError(ex.Message);
				startNextSignature();
				return;

			}

			// Store the signature file on the folder "App_Data/" and redirects to the SignatureInfo action with the filename.
			// With this filename, it can show a link to download the signature file.
			var file = Storage.StoreFile(signatureContent, ".pdf");

			var docItem = DocumentsListView.Items[DocumentIndex];
			docItem.DataItem = new DocumentItem() {
				Id = DocumentIds[DocumentIndex],
				DownloadLink = "Download?file=" + file
			};
			docItem.DataBind();

			startNextSignature();
		}


		private void setValidationError(ValidationResults vr) {
			var message = "One or more validations failed: " + string.Join("; ", vr.Errors.Select(e => getDisplayText(e)));
			setError(message);
		}

		private void setError(string message) {
			var docItem = DocumentsListView.Items[DocumentIndex];
			docItem.DataItem = new DocumentItem() {
				Id = DocumentIds[DocumentIndex],
				Error = message
			};
			docItem.DataBind();
		}

		private string getDisplayText(ValidationItem vi) {
			return string.IsNullOrEmpty(vi.Detail) ? vi.Message : string.Format("{0} ({1})", vi.Message, vi.Detail);
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
