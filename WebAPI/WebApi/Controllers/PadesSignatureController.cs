using Lacuna.Pki;
using Lacuna.Pki.Pades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using WebApi.Classes;
using WebApi.Models;

namespace WebApi.Controllers {
	public class PadesSignatureController : ApiController {

		/**
			This method defines the signature policy that will be used on the signature.
		 */
		private IPadesPolicyMapper getSignaturePolicy() {
			return PadesPoliciesForGeneration.GetPadesBasic(Util.GetTrustArbitrator());
		}

		/**
			This method defines the visual representation for each signature. For more information, see
			http://pki.lacunasoftware.com/Help/html/98095ec7-2742-4d1f-9709-681c684eb13b.htm
		 */
		private PadesVisualRepresentation2 getVisualRepresentation(PKCertificate cert) {
			return new PadesVisualRepresentation2() {

				// Text of the visual representation
				Text = new PadesVisualText() {

					// used to compose the message
					CustomText = String.Format("Digitally signed by {0}", cert.SubjectName.CommonName),

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
		}

		[HttpPost, Route("Api/PadesSignature/Start")]
		public IHttpActionResult Start(SignatureStartRequest request) {

			byte[] toSignBytes, transferData;
			SignatureAlgorithm signatureAlg;

			try {

				// Decode the user's certificate
				var cert = PKCertificate.Decode(request.Certificate);

				// Instantiate a PadesSigner class
				var padesSigner = new PadesSigner();

				// Set the PDF to sign, which in the case of this example is a fixed sample document
				padesSigner.SetPdfToSign(Storage.GetSampleDocContent());

				// Set the signer certificate
				padesSigner.SetSigningCertificate(cert);

				// Set the signature policy
				padesSigner.SetPolicy(getSignaturePolicy());

				// Set the signature's visual representation options (optional)
				padesSigner.SetVisualRepresentation(getVisualRepresentation(cert));

				// Generate the "to-sign-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy, as well as the "transfer data",
				// a byte-array that will be needed on the next step.
				toSignBytes = padesSigner.GetToSignBytes(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired.
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}

			// Create response with some informations that we'll use on Complete action and on client-side.
			var response = new SignatureStartResponse() {
				// The "transfer data" for PDF signatures can be as large as the original PDF itself. Therefore, we mustn't use a hidden field
				// on the page to store it. Here we're using our storage mock (see file Classes\Storage.cs) to simulate storing the transfer data
				// on a database and saving on a hidden field on the page only the ID that can be used later to retrieve it. Another option would
				// be to store the transfer data on the Session dictionary.
				TransferDataFileId = Storage.StoreFile(transferData),

				// Send to the javascript the "to sign hash" of the document (digest of the "to-sign-bytes") and the digest algorithm that must
				// be used on the signature algorithm computation
				ToSignBytes = toSignBytes,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid
			};

			return Ok(response);
		}

		[HttpPost, Route("Api/PadesSignature/Complete")]
		public IHttpActionResult Complete(SignatureCompleteRequest request) {

			byte[] signatureContent;

			try {

				// Retrieve the "transfer data" stored on the initial step (see Start action)
				var transferData = Storage.GetFile(request.TransferDataFileId);

				// We won't be needing the "transfer data" anymore, so we delte it
				Storage.DeleteFile(request.TransferDataFileId);

				// Instantiate a PadesSigner class
				var padesSigner = new PadesSigner();

				// Set the signature policy, exactly like in the Start method
				padesSigner.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data"
				padesSigner.SetPreComputedSignature(request.Signature, transferData);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				padesSigner.ComputeSignature();

				// Get the signed PDF as an array of bytes
				signatureContent = padesSigner.GetPadesSignature();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}

			// Pass the following fields to be used on signature-results template:
			// - The signature file will be stored on the folder "App_Data/". Its name will be passed by Filename field.
			// - The user's certificate
			var response = new SignatureCompleteResponse() {
				Filename = Storage.StoreFile(signatureContent, ".pdf"),
				Certificate = new CertificateModel(PKCertificate.Decode(request.Certificate))
			};

			return Ok(response);
		}
	}
}