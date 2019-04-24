using Lacuna.Pki;
using Lacuna.Pki.Cades;
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
	public class CadesSignatureController : ApiController {

		/**
			This method defines the signature policy that will be used on the signature.
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

		[HttpPost, Route("Api/CadesSignature/Start")]
		public IHttpActionResult Start(SignatureStartRequest request) {

			byte[] toSignBytes;
			SignatureAlgorithm signatureAlg;

			try {

				// Decode the user's certificate
				var cert = PKCertificate.Decode(request.Certificate);

				// Instantiate a CadesSigner class
				var cadesSigner = new CadesSigner();

				// Set the data to sign, which in the case of this example is a fixed sample document
				if (!string.IsNullOrEmpty(request.FileId)) {
					cadesSigner.SetDataToSign(Storage.GetFile(request.FileId));
				} else {
					cadesSigner.SetDataToSign(Storage.GetSampleDocContent());
				}

				// Set the signer certificate
				cadesSigner.SetSigningCertificate(cert);

				// Set the signature policy
				cadesSigner.SetPolicy(getSignaturePolicy());

				// Generate the "to-sign-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignBytes = cadesSigner.GenerateToSignBytes(out signatureAlg);


			} catch (ValidationException ex) {

				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired.
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}

			// Create response with some informations that we'll use on Complete action and on client-side.
			var response = new SignatureStartResponse() {
				// Send to the javascript the "to sign hash" of the document and the digest algorithm that must
				// be used on the signature algorithm computation
				ToSignBytes = toSignBytes,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid
			};

			return Ok(response);
		}

		[HttpPost, Route("Api/CadesSignature/Complete")]
		public IHttpActionResult Complete(SignatureCompleteRequest request) {

			byte[] signatureContent;

			try {

				// Instantiate a CadesSigner class
				var cadesSigner = new CadesSigner();

				// Set the document to be signed and the policy, exactly like in the Start action
				if (!string.IsNullOrEmpty(request.FileId)) {
					cadesSigner.SetDataToSign(Storage.GetFile(request.FileId));
				} else {
					cadesSigner.SetDataToSign(Storage.GetSampleDocContent());
				}
				
				cadesSigner.SetPolicy(getSignaturePolicy());

				// Set signer's certificate
				cadesSigner.SetSigningCertificate(PKCertificate.Decode(request.Certificate));

				// Set the signature computed on the client-side, along with the "to-sign-bytes" received from the request.
				cadesSigner.SetPrecomputedSignature(request.Signature, request.ToSignBytes);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature.
				cadesSigner.ComputeSignature();

				// Get the signature as an array of bytes
				signatureContent = cadesSigner.GetSignature();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}

			// Pass the following fields to be used on signature-results template:
			// - The signature file will be stored on the folder "App_Data/". Its name will be passed by Filename field.
			// - The user's certificate
			var response = new SignatureCompleteResponse() {
				Filename = Storage.StoreFile(signatureContent, ".p7s"),
				Certificate = new CertificateModel(PKCertificate.Decode(request.Certificate))
			};

			return Ok(response);

		}
	}
}