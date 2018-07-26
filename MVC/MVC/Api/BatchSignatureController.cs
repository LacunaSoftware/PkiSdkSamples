﻿using Lacuna.Pki;
using Lacuna.Pki.Pades;
using MVC.Api.Models;
using MVC.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MVC.Api {
	public class BatchSignatureController : ApiController {

		/**
		 * This method defines the signature policy that will be used on the signature.
		 */
		private IPadesPolicyMapper getSignaturePolicy() {

#if DEBUG
			// During debug only, we return a wrapper which will overwrite the policy's default trust arbitrator (which in this case
			// corresponds to the ICP-Brasil roots only), with our custom trust arbitrator which accepts test certificates
			// (see Util.GetTrustArbitrator())
			return PadesPoliciesForGeneration.GetPadesBasic(Util.GetTrustArbitrator());
#else
			return PadesPoliciesForGeneration.GetPadesBasic(TrustArbitrators.PkiBrazil);
#endif
		}

		/**
		 * This action is called asynchronously from the batch signature page in order to initiate the signature of each document
		 * in the batch.
		 */
		[HttpPost]
		public IHttpActionResult Start(BatchSignatureStartRequest request) {

			byte[] toSignBytes, transferData;
			SignatureAlgorithm signatureAlg;

			try {

				// Decode the user's certificate
				var cert = PKCertificate.Decode(request.CertContent);

				// Instantiate a PadesSigner class
				var padesSigner = new PadesSigner();

				// Set the PDF to sign, which in the case of this example is one of the batch documents
				padesSigner.SetPdfToSign(Storage.GetBatchDocContent(request.Id));

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

						// We'll use as background the image in Content/PdfStamp.png
						Content = Storage.GetPdfStampContent(),

						// Opacity is an integer from 0 to 100 (0 is completely transparent, 100 is completely opaque).
						Opacity = 50,

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

			}
			catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired.
				var message = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.ValidationResults.ToString());
				return ResponseMessage(message);
			}

			// For the next steps, we'll need once again some information:
			// - The "transfer data" filename. Its content is stored in a temporary file (with extension .bin) to
			// be shared with the Complete action.
			// - The "to-sign-hash" (digest of the "to-sign-bytes"). And the OID of the digest algorithm to be 
			// used during the signature operation. this information is need in the signature computation with
			// Web PKI component. (see batch-signature-form.js)
			return Ok(new BatchSignatureStartResponse() {
				TransferDataFileId = Storage.StoreFile(transferData, ".bin"),
				ToSignHash = signatureAlg.DigestAlgorithm.ComputeHash(toSignBytes),
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid
			});
		}

		/**
		 * POST: Api/BatchSignature/Complete
		 * 
		 * This action is called once the "to-sign-hash" are signed using the user's certificate. After signature,
		 * it'll be redirect to SignatureInfo action to show the signature file.
		 */
		[HttpPost]
		public IHttpActionResult Complete(BatchSignatureCompleteRequest request) {

			byte[] signatureContent;

			try {

				// Recover the "transfer data" content stored in a temporary file
				string extension;
				byte[] transferDataContent;
				if (!Storage.TryGetFile(request.TransferDataFileId, out transferDataContent, out extension)) {
					return NotFound();
				}

				// Instantiate a PadesSigner class
				var padesSigner = new PadesSigner();

				// Set the signature policy, exactly like in the Start method
				padesSigner.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data" recovered from a temporary file
				padesSigner.SetPreComputedSignature(request.Signature, transferDataContent);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the 
				// resulting signature
				padesSigner.ComputeSignature();

				// Get the signed PDF as an array of bytes
				signatureContent = padesSigner.GetPadesSignature();

			}
			catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				var message = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.ValidationResults.ToString());
				return ResponseMessage(message);
			}

			return Ok(new BatchSignatureCompleteResponse() {
				SignedFileId = Storage.StoreFile(signatureContent, ".pdf")
			});
		}
	}
}
