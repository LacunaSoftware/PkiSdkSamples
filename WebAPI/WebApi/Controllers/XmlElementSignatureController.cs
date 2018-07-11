using Lacuna.Pki;
using Lacuna.Pki.Xml;
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
	public class XmlElementSignatureController : ApiController {

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

		[HttpPost, Route("Api/XmlElementSignature/Start")]
		public IHttpActionResult Start(SignatureStartRequest request) {

			byte[] toSignHash, transferData;
			SignatureAlgorithm signatureAlg;

			try {

				// Decode the user's certificate
				var cert = PKCertificate.Decode(request.Certificate);

				// Instantiate a XmlElementSigner class
				var signer = new XmlElementSigner();

				// Set the data to sign, which in the case of this example is a fixed sample document
				signer.SetXml(Storage.GetSampleNFeContent());

				// static Id from node <infNFe> from SampleNFe.xml document
				signer.SetToSignElementId("NFe35141214314050000662550010001084271182362300");

				// Set the signer certificate decoded
				signer.SetSigningCertificate(cert);

				// Set the signature policy
				signer.SetPolicy(getSignaturePolicy());

				// Generate the "to-sign-hash". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy. As well as the "transfer data", 
				// a byte-array that will be needed on the next step.
				toSignHash = signer.GenerateToSignHash(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired. 
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}

			// Create response with some informations that we'll use on Complete action and on client-side.
			var response = new SignatureStartResponse() {
				// The "transfer data" for Xml signatyures are not so big. Therefore, we can easily send it as one of
				// response fields.
				TransferData = transferData,

				// Send to the javascript the "to sign hash" of the document and the digest algorithm that must
				// be used on the signature algorithm computation
				ToSignHash = toSignHash,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid
			};

			return Ok(response);
		}

		[HttpPost, Route("Api/XmlElementSignature/Complete")]
		public IHttpActionResult Complete(SignatureCompleteRequest request) {

			byte[] signatureContent;

			try {

				// Instantiate a XmlElementSigner class
				var signer = new XmlElementSigner();

				// Set the document to be signed and the policy, exactly like in the Start action
				signer.SetXml(Storage.GetSampleNFeContent());
				signer.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data"
				signer.SetPrecomputedSignature(request.Signature, request.TransferData);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				signer.ComputeSignature();

				// Get the signed XML as an array of bytes
				signatureContent = signer.GetSignedXml();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}

			// Pass the following fields to be used on signature-results template:
			// - The signature file will be stored on the folder "App_Data/". Its name will be passed by Filename field.
			// - The user's certificate
			var response = new SignatureCompleteResponse() {
				Filename = Storage.StoreFile(signatureContent, ".xml"),
				Certificate = new CertificateModel(PKCertificate.Decode(request.Certificate))
			};

			return Ok(response);
		}
	}
}