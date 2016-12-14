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
    public class XmlElementSignatureController : Controller {

        // GET: XmlElementSignature
		[HttpGet]
        public ActionResult Index() {

            return View();
        }

		/**
		 * POST: XmlElementSignature
		 * 
		 * This action is called once the user's certificate encoding has been read, and contains the
		 * logic to prepare the byte array that needs to be actually signed with the user's private key
		 * (the "to-sign-hash-bytes").
		 */
		[HttpPost]
		public ActionResult Index(SignatureStartModel model) {

			byte[] toSignHash, transferData;
			SignatureAlgorithm signatureAlg;

			try {
				// Instantiate a CadesSigner class
				var signer = new XmlElementSigner();

				// Set the data to sign, which in the case of this example is a fixed sample document
				signer.SetXml(Storage.GetSampleNFeContent());

				// static Id from node <infNFe> from SampleNFe.xml document
				signer.SetToSignElementId("NFe35141214314050000662550010001084271182362300");

				// Decode the user's certificate and set as the signer certificate
				signer.SetSigningCertificate(PKCertificate.Decode(model.CertContent));

				// Set the signature policy
				signer.SetPolicy(Util.GetXmlSignaturePolicy());

				// Generate the "to-sign-hash-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignHash = signer.GenerateToSignHash(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired. 
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// On the next step (Complete action), we'll need once again some information:
			// - The thumpprint of the selected certificate
			// - The "to-sign-bytes"
			// - The OID of the digest algorithm to be used during the signature operation
			// - The "transfer data"
			// We'll store this value on TempData, that will store in dictionary shared between actions.
			TempData["SignatureCompleteModel"] = new SignatureCompleteModel() {
				CertThumb = model.CertThumb,
				ToSignHash = toSignHash,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid,
				TransferData = transferData
			};

			return RedirectToAction("Complete");
		}

		// GET: XmlElementSignature/Complete
		[HttpGet]
		public ActionResult Complete() {

			// Recovery data from Start() action, if returns null, it'll be redirected to Index 
			// action again.
			var model = TempData["SignatureCompleteModel"] as SignatureCompleteModel;
			if (model == null) {
				return RedirectToAction("Index");
			}

			return View(model);
		}

		/**
		 * POST: XmlElementSignature/Complete
		 * 
		 * This action is called once the "to-sign-bytes" are signed using the user's certificate. After signature,
		 * it'll be redirect to SignatureInfo action to show the signature file.
		 */
		[HttpPost]
		public ActionResult Complete(SignatureCompleteModel model) {

			byte[] signatureContent;

			try {
				var signer = new XmlElementSigner();

				// Set the document to be signed and the policy, exactly like in the Start method
				signer.SetXml(Storage.GetSampleNFeContent());
				signer.SetPolicy(Util.GetXmlSignaturePolicy());

				// Set the signature computed on the client-side, along with the "to-sign-bytes" recovered from the database
				signer.SetPrecomputedSignature(model.Signature, model.TransferData);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				signer.ComputeSignature();

				// Get the signed XML as an array of bytes
				signatureContent = signer.GetSignedXml();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// Store the signature file on the folder "App_Data/" and redirects to the SignatureInfo action with the filename.
			// With this filename, it can show a link to download the signature file.
			var extension = ".xml";
			var filename = Storage.StoreFile(signatureContent, extension).Replace('.', '_');
			return RedirectToAction("SignatureInfo", new SignatureInfoModel() {
				File = filename
			});
		}

		// GET: XmlElementSignature/SignatureInfo
		[HttpGet]
		public ActionResult SignatureInfo(SignatureInfoModel model) {
			return View(model);
		}
    }
}
