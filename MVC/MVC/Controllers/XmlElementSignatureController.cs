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

            return View("Index3");
        }

		// POST: XmlElementSignature
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
				signer.SetSigningCertificate(PKCertificate.Decode(Convert.FromBase64String(model.CertContent)));

				// Set the signature policy
				signer.SetPolicy(MVC.Classes.PkiUtil.GetXmlSignaturePolicy());

				// Generate the "to-sign-hash-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignHash = signer.GenerateToSignHash(out signatureAlg, out transferData);

			} catch (ValidationException ex) {
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View("Index3");
			}

			TempData["SignatureCompleteModel"] = new SignatureCompleteModel() {
				CertThumb = model.CertThumb,
				ToSignHash = Convert.ToBase64String(toSignHash),
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid,
				TransferData = Convert.ToBase64String(transferData)
			};

			return RedirectToAction("Complete");
		}

		// GET: XmlElementSignature/Complete
		[HttpGet]
		public ActionResult Complete() {

			var model = TempData["SignatureCompleteModel"] as SignatureCompleteModel;
			if (model == null) {
				return RedirectToAction("Index");
			}

			return View("Complete3", model);
		}

		// POST: XmlElementSignature/Complete
		[HttpPost]
		public ActionResult Complete(SignatureCompleteModel model) {

			byte[] signatureContent;

			try {
				var signer = new XmlElementSigner();

				// Set the document to be signed and the policy, exactly like in the Start method
				signer.SetXml(Storage.GetSampleNFeContent());
				signer.SetPolicy(MVC.Classes.PkiUtil.GetXmlSignaturePolicy());

				// Set the signature computed on the client-side, along with the "to-sign-bytes" recovered from the database
				signer.SetPrecomputedSignature(Convert.FromBase64String(model.Signature), Convert.FromBase64String(model.TransferData));

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				signer.ComputeSignature();

				// Get the signed XML as an array of bytes
				signatureContent = signer.GetSignedXml();

			} catch (ValidationException ex) {
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View("Complete3");
			}

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
