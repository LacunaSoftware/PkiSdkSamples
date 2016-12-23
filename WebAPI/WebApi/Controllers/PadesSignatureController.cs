﻿using Lacuna.Pki;
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
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired.
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}

			// On the next step (Complete action), we'll need once again some information:
			// - The content of the selected certificate used to validate the signature in complete action.
			// - The thumbprint of the selected certificate
			// - The "to-sign-bytes"
			// - The "to-sign-hash" (digest of the "to-sign-bytes")
			// - The OID of the digest algorithm to be used during the signature operation
			// We'll store these values on TempData, which is a dictionary shared between actions.
			var response = new SignatureStartResponse() {
				ToSignBytes = toSignBytes,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid,
				TransferData = transferData
			};

			return Ok(response);
		}

		[HttpPost, Route("Api/PadesSignature/Complete")]
		public IHttpActionResult Complete(SignatureCompleteRequest request) {

			byte[] signatureContent;

			try {

				var padesSigner = new PadesSigner();

				// Set the signature policy, exactly like in the Start method
				padesSigner.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data" recovered from the database
				padesSigner.SetPreComputedSignature(request.Signature, request.TransferData);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				padesSigner.ComputeSignature();

				// Get the signed PDF as an array of bytes
				signatureContent = padesSigner.GetPadesSignature();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}
			
			// On the next step (Complete action), we'll need once again some information:
			// - The content of the selected certificate used to validate the signature in complete action.
			// - The filename to be available to download in next action.
			// We'll store these values on TempData, which is a dictionary shared between actions.
			var response = new SignatureCompleteResponse() {
				// Store the signature file on the folder "App_Data/" and redirects to the SignatureInfo action with the filename.
				// With this filename, it can show a link to download the signature file.
				Filename = Storage.StoreFile(signatureContent, ".pdf"),
				Certificate = new CertificateModel(PKCertificate.Decode(request.Certificate))
			};

			return Ok(response);
		}
	}
}