using Lacuna.Pki;
using Lacuna.Pki.Cades;
using Lacuna.Pki.Pades;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {

	public partial class BatchCadesSignature : System.Web.UI.Page {

		private static Logger logger = LogManager.GetCurrentClassLogger();

		// Class used to display each of the batch's documents on the page
		class DocumentItem {
			public int Id { get; set; }
			public string Error { get; set; }
			public string DownloadLink { get; set; }
		}

		/*
			We store the IDs of the batch's documents in the hidden field "DocumentIdsField". Since we don't need this data
			on the Javascript, we could alternatively store it on the Session dictionary
		 */
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

		/*
			We store the index of the document currently being signed on the hidden field "DocumentIndexField". Since we don't need
			this data on the Javascript, we could alternatively store it on the Session dictionary
		 */
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
				
				// It is up to your application's business logic to determine which documents will compose the batch
				DocumentIds = Enumerable.Range(1, 30).ToList(); // from 1 to 30

				// Populate the DocumentsListView with the batch documents
				DocumentsListView.DataSource = DocumentIds.ConvertAll(i => new DocumentItem() { Id = i });
				DocumentsListView.DataBind();
			}
		}

		// The button "SubmitCertificateButton" is programmatically clicked by the Javascript on batch-signature-form.js when the
		// selected certificate's encoding has been retrieved
		protected void SubmitCertificateButton_Click(object sender, EventArgs e) {
			
			// Hide the certificate select (combo box) and the signature buttons
			SignatureControlsPanel.Visible = false;
			
			// Start the signature of the first document in the batch
			DocumentIndex = -1;
			startNextSignature();
		}

		// The button "SubmitSignatureButton" is programmatically clicked by the Javascript on batch-signature-form.js when the
		// "to sign hash" of the current document has been signed with the certificate's private key
		protected void SubmitSignatureButton_Click(object sender, EventArgs e) {

			// Complete the signature
			completeSignature();

			// Start the next signature
			startNextSignature();
		}

		private void startNextSignature() {

			// Increment the index of the document currently being signed
			DocumentIndex += 1;

			// Check if we have reached the end of the batch, in which case we fill the hidden field "ToSignHashField" with value "(end)",
			// which signals to the javascript on batch-signature-form.js that the process is completed and the page can be unblocked.
			if (DocumentIndex == DocumentIds.Count) {
				ToSignHashField.Value = "(end)";
				return;
			}

			// Get the ID of the document currently being signed
			var docId = DocumentIds[DocumentIndex];

			byte[] toSignBytes;
			SignatureAlgorithm signatureAlg;

			try {

                // Decode the user's certificate
                var cert = PKCertificate.Decode(Convert.FromBase64String(CertificateField.Value));

                // Instantiate a CadesSigner class
                var cadesSigner = new CadesSigner();

                // Set the data to sign, which in the case of this example is a fixed sample document
                cadesSigner.SetDataToSign(Storage.GetBatchDocContent(docId));

                // Set the signer certificate
                cadesSigner.SetSigningCertificate(cert);

                // Set the signature policy
                cadesSigner.SetPolicy(getSignaturePolicy());

                // Generate the "to-sign-bytes". This method also yields the signature algorithm that must
                // be used on the client-side, based on the signature policy.
                toSignBytes = cadesSigner.GenerateToSignBytes(out signatureAlg);

            } catch (ValidationException ex) {

				// One or more validations failed. We log the error, update the page with a summary of what happened to this document and start the next signature
				logger.Error(ex, "Validation error starting the signature of a batch document");
				setValidationError(ex.ValidationResults);
				startNextSignature();
				return;

			} catch (Exception ex) {

				// An error has occurred. We log the error, update the page with a summary of what happened to this document and start the next signature
				logger.Error(ex, "Error starting the signature of a batch document");
				setError(ex.Message);
				startNextSignature();
				return;

			}

            // Send to the javascript the "to sign hash" of the document (digest of the "to-sign-bytes") and the digest algorithm that must
            // be used on the signature algorithm computation
            ToSignBytesField.Value = Convert.ToBase64String(toSignBytes);
            ToSignHashField.Value = Convert.ToBase64String(signatureAlg.DigestAlgorithm.ComputeHash(toSignBytes));
			DigestAlgorithmField.Value = signatureAlg.DigestAlgorithm.Oid;
		}

		private void completeSignature() {

            PKCertificate cert;
            byte[] signatureContent;

			try {

                // Decode the user's certificate
                cert = PKCertificate.Decode(Convert.FromBase64String(CertificateField.Value));

                // Instantiate a CadesSigner class
                var cadesSigner = new CadesSigner();

                // Set the document to be signed and the policy, exactly like in the previous action (SubmitCertificateButton_Click)
                cadesSigner.SetDataDigestToSign(DigestAlgorithm.GetInstanceByOid(DigestAlgorithmField.Value), Convert.FromBase64String(ToSignHashField.Value));
                cadesSigner.SetPolicy(getSignaturePolicy());

                // Set the signer certificate
                cadesSigner.SetSigningCertificate(cert);

                // Set the signature computed on the client-side, along with the "to-sign-bytes" recovered from the page
                cadesSigner.SetPrecomputedSignature(Convert.FromBase64String(SignatureField.Value), Convert.FromBase64String(ToSignBytesField.Value));

                // Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
                cadesSigner.ComputeSignature();

                // Get the signature as an array of bytes
                signatureContent = cadesSigner.GetSignature();

            } catch (ValidationException ex) {

				// One or more validations failed. We log the error and update the page with a summary of what happened to this document
				logger.Error(ex, "Validation error completing the signature of a batch document");
				setValidationError(ex.ValidationResults);
				return;

			} catch (Exception ex) {

				// An error has occurred. We log the error and update the page with a summary of what happened to this document
				logger.Error(ex, "Error completing the signature of a batch document");
				setError(ex.Message);
				return;

			}

            // Store the signed file
            var file = Storage.StoreFile(signatureContent, ".p7s");

            // Update the page with a link to the signed file
            var docItem = DocumentsListView.Items[DocumentIndex];
			docItem.DataItem = new DocumentItem() {
				Id = DocumentIds[DocumentIndex],
				DownloadLink = "Download?file=" + file
			};
			docItem.DataBind();
		}

		private void setValidationError(ValidationResults vr) {
			var message = "One or more validations failed: " + string.Join("; ", vr.Errors.Select(e => getDisplayText(e)));
			setError(message);
		}

		private string getDisplayText(ValidationItem vi) {
			return string.IsNullOrEmpty(vi.Detail) ? vi.Message : string.Format("{0} ({1})", vi.Message, vi.Detail);
		}

		private void setError(string message) {
			var docItem = DocumentsListView.Items[DocumentIndex];
			docItem.DataItem = new DocumentItem() {
				Id = DocumentIds[DocumentIndex],
				Error = message
			};
			docItem.DataBind();
		}

        /**
		 *	This method defines the signature policy that will be used on the signature.
		 */
        private ICadesPolicyMapper getSignaturePolicy()
        {

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
