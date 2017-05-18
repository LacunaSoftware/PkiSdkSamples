using Lacuna.Pki;
using Lacuna.Pki.Xml;
using MVC.Classes;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVC.Controllers {

	/**
	 * This controller performs two signatures on the same XML document, one on each element, according to the standard Certificación de Origen Digital (COD),
	 * from Asociación Latinoamericana de Integración (ALADI). For more information, please see:
	 * 
	 * - Spanish: http://www.aladi.org/nsfweb/Documentos/2327Rev2.pdf
	 * - Portuguese: http://www.mdic.gov.br/images/REPOSITORIO/secex/deint/coreo/2014_09_19_-_Brasaladi_761_-_Documento_ALADI_SEC__di_2327__Rev_2_al_port_.pdf
	 */
	public class CodXmlSignatureController : Controller {

		/**
		 * This method defines the signature policy that will be used on the signatures.
		 */
 		private XmlPolicySpec getSignaturePolicy() {
			var policy = XmlPolicySpec.GetXmlDSigBasic(Util.GetTrustArbitrator(), DigestAlgorithm.SHA1);
			// Optionally customize policy. The customizations below are a suggestion based on existing signed COD XML documents.
			policy.Generation.XmlTransformations.Clear();
			policy.Generation.XmlTransformations.Add(XmlTransformation.EnvelopedSignature);
			policy.Generation.OmitSignatureElementIds = true;
			policy.Generation.IncludeKeyValue = true;
			policy.Generation.X509DataCertificates = InclusionLevel.SigningCertificateOnly;
			policy.Generation.X509DataFields = X509DataFields.X509SubjectName;
			return policy;
		}

		public ActionResult Index() {
			return View();
		}

		/**
		 * GET CodXmlSignature/SignCod
		 * 
		 * Renders the first signature page (for the COD element)
		 */
		public ActionResult SignCod() {
			return View();
		}

		/**
		 * POST CodXmlSignature/SignCod
		 * 
		 * This action is called once the user's certificate encoding has been read, and contains the
		 * logic to prepare the COD element to be signed, yielding the byte array that needs to be 
		 * actually signed with the user's private key (the "to-sign-hash-bytes").
		 */
		[HttpPost]
		public ActionResult SignCod(SignatureStartModel model) {

			byte[] toSignHash, transferData;
			SignatureAlgorithm signatureAlg;

			try {
				// Instantiate a CadesSigner class
				var signer = new XmlElementSigner();

				// Set the data to sign, which in the case of this example is a fixed sample "COD envelope"
				signer.SetXml(Storage.GetSampleCodEnvelope());

				// Set the ID of the COD element
				signer.SetToSignElementId("COD");

				// Decode the user's certificate and set as the signer certificate
				signer.SetSigningCertificate(PKCertificate.Decode(model.CertContent));

				// Set the signature policy
				signer.SetPolicy(getSignaturePolicy());

				// Generate the "to-sign-hash-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignHash = signer.GenerateToSignHash(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired. 
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// On the next step (SignCodComplete action), we'll need once again some information:
			// - The thumpprint of the selected certificate
			// - The "to-sign-hash"
			// - The OID of the digest algorithm to be used during the signature operation
			// - The "transfer data"
			// We'll store this value on TempData, that will store in dictionary shared between actions.
			TempData["SignatureCompleteModel"] = new SignatureCompleteModel() {
				CertThumb = model.CertThumb,
				ToSignHash = toSignHash,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid,
				TransferData = transferData
			};

			return RedirectToAction("SignCodComplete");
		}

		/**
		 * GET CodXmlSignature/SignCodComplete
		 * 
		 * Renders the page on which the signature of the COD element will actually be computed
		 * using the "to-sign-hash" generated on the SignCod action
		 */
		[HttpGet]
		public ActionResult SignCodComplete() {

			// Recover data from StartCodSignature action
			var model = TempData["SignatureCompleteModel"] as SignatureCompleteModel;
			if (model == null) {
				return RedirectToAction("Index");
			}

			return View(model);
		}

		/**
		 * POST CodXmlSignature/SignCodComplete
		 * 
		 * This action is called once the "to-sign-hash" is signed using the user's certificate. After signature,
		 * we'll redirect the user to the SignCodResult action to show the signed file.
		 */
		[HttpPost]
		public ActionResult SignCodComplete(SignatureCompleteModel model) {

			byte[] signatureContent;

			try {
				var signer = new XmlElementSigner();

				// Set the document to be signed and the policy, exactly like in the SignCod method
				signer.SetXml(Storage.GetSampleCodEnvelope());
				signer.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data"
				signer.SetPrecomputedSignature(model.Signature, model.TransferData);

				// It is not necessary to set the signing certificate nor the element ID to be signed, both are contained in the "transfer data"

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				signer.ComputeSignature();

				// Get the signed XML as an array of bytes
				signatureContent = signer.GetSignedXml();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// Store the signature file on the folder "App_Data/" and redirect to the SignCodResult action with the filename.
			var file = Storage.StoreFile(signatureContent, ".xml");
			return RedirectToAction("SignCodResult", new SignatureInfoModel() {
				File = file
			});
		}

		/**
		 * GET CodXmlSignature/SignCodResult
		 */
		[HttpGet]
		public ActionResult SignCodResult(SignatureInfoModel model) {
			return View(model);
		}

		/**
		 * GET CodXmlSignature/SignCodeh
		 * 
		 * Renders the second signature page (for the CODEH element)
		 */
		public ActionResult SignCodeh(string id) {
			var model = new SignatureStartModel() {
				File = id
			};
			return View(model);
		}

		/**
		 * POST CodXmlSignature/SignCodeh
		 * 
		 * This action is called once the user's certificate encoding has been read, and contains the
		 * logic to prepare the CODEH element to be signed, yielding the byte array that needs to be 
		 * actually signed with the user's private key (the "to-sign-hash-bytes").
		 */
		[HttpPost]
		public ActionResult SignCodeh(string id, SignatureStartModel model) {

			// Recover XML envelope with signed COD element from "storage" based on its ID
			byte[] content;
			string extension;
			if (!Storage.TryGetFile(id, out content, out extension)) {
				return HttpNotFound();
			}

			byte[] toSignHash, transferData;
			SignatureAlgorithm signatureAlg;

			try {
				// Instantiate a CadesSigner class
				var signer = new XmlElementSigner();

				// Set the XML to sign
				signer.SetXml(content);

				// Set the ID of the CODEH element
				signer.SetToSignElementId("CODEH");

				// Decode the user's certificate and set as the signer certificate
				signer.SetSigningCertificate(PKCertificate.Decode(model.CertContent));

				// Set the signature policy
				signer.SetPolicy(getSignaturePolicy());

				// Generate the "to-sign-hash-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignHash = signer.GenerateToSignHash(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired. 
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// On the next step (SignCodComplete action), we'll need once again some information:
			// - The thumpprint of the selected certificate
			// - The "to-sign-hash"
			// - The OID of the digest algorithm to be used during the signature operation
			// - The "transfer data"
			// We'll store this value on TempData, that will store in dictionary shared between actions.
			TempData["SignatureCompleteModel"] = new SignatureCompleteModel() {
				CertThumb = model.CertThumb,
				ToSignHash = toSignHash,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid,
				TransferData = transferData
			};

			return RedirectToAction("SignCodehComplete", new { id });
		}

		/**
		 * GET CodXmlSignature/SignCodehComplete
		 * 
		 * Renders the page on which the signature of the CODEH element will actually be computed
		 * using the "to-sign-hash" generated on the SignCodeh action
		 */
		[HttpGet]
		public ActionResult SignCodehComplete(string id) {

			// Recover data from StartCodSignature action
			var model = TempData["SignatureCompleteModel"] as SignatureCompleteModel;
			if (model == null) {
				return RedirectToAction("Index");
			}

			return View(model);
		}

		/**
		 * POST CodXmlSignature/SignCodehComplete
		 * 
		 * This action is called once the "to-sign-hash" is signed using the user's certificate. After signature,
		 * we'll redirect the user to the SignCodehResult action to show the signed file.
		 */
		[HttpPost]
		public ActionResult SignCodehComplete(string id, SignatureCompleteModel model) {

			// Recover XML envelope with signed COD element from "storage" based on its ID
			byte[] content;
			string extension;
			if (!Storage.TryGetFile(id, out content, out extension)) {
				return HttpNotFound();
			}

			byte[] signatureContent;

			try {
				var signer = new XmlElementSigner();

				// Set the document to be signed and the policy, exactly like in the SignCodeh method
				signer.SetXml(content);
				signer.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data"
				signer.SetPrecomputedSignature(model.Signature, model.TransferData);

				// It is not necessary to set the signing certificate nor the element ID to be signed, both are contained in the "transfer data"

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				signer.ComputeSignature();

				// Get the signed XML as an array of bytes
				signatureContent = signer.GetSignedXml();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// Store the signature file on the folder "App_Data/" and redirect to the SignCodehResult action with the filename.
			var file = Storage.StoreFile(signatureContent, ".xml");
			return RedirectToAction("SignCodehResult", new SignatureInfoModel() {
				File = file
			});
		}

		/**
		 * GET CodXmlSignature/SignCodehResult
		 */
		[HttpGet]
		public ActionResult SignCodehResult(SignatureInfoModel model) {
			return View(model);
		}

	}
}
