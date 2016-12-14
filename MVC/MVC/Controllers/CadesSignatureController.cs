using Lacuna.Pki;
using Lacuna.Pki.Cades;
using MVC.Classes;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVC.Controllers {
	public class CadesSignatureController : Controller {
		// GET: CadesSignature
		[HttpGet]
		public ActionResult Index() {

			return View();
		}

		// POST: CadesSignature
		[HttpPost]
		public ActionResult Index(SignatureStartModel model) {
			byte[] toSignBytes;
			SignatureAlgorithm signatureAlg;

			try {

				// Instantiate a CadesSigner class
				var cadesSigner = new CadesSigner();

				// Set the data to sign, which in the case of this example is a fixed sample document
				cadesSigner.SetDataToSign(Storage.GetSampleDocContent());

				// Decode the user's certificate and set as the signer certificate
				cadesSigner.SetSigningCertificate(PKCertificate.Decode(Convert.FromBase64String(model.CertContent)));

				// Set the signature policy
				cadesSigner.SetPolicy(MVC.Classes.PkiUtil.GetCadesSignaturePolicy());

				// Generate the "to-sign-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignBytes = cadesSigner.GenerateToSignBytes(out signatureAlg);
				

			} catch (ValidationException ex) {
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			TempData["SignatureCompleteModel"] = new SignatureCompleteModel() {
				CertContent = model.CertContent,
				CertThumb = model.CertThumb,
				ToSignBytes = Convert.ToBase64String(toSignBytes),
				ToSignHash = Convert.ToBase64String(signatureAlg.DigestAlgorithm.ComputeHash(toSignBytes)),
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid
			};

			return RedirectToAction("Complete");
		}

		// GET: CadesSignature/Complete
		[HttpGet]
		public ActionResult Complete() {

			var model = TempData["SignatureCompleteModel"] as SignatureCompleteModel;
			if (model == null) {
				return RedirectToAction("Index");
			}

			return View(model);
		}

		// POST: CadesSignature/Complete
		[HttpPost]
		public ActionResult Complete(SignatureCompleteModel model) {

			byte[] signatureContent;
			try {

				var cadesSigner = new CadesSigner();

				// Set the document to be signed and the policy, exactly like in the Start method
				cadesSigner.SetDataToSign(Storage.GetSampleDocContent());
				cadesSigner.SetPolicy(MVC.Classes.PkiUtil.GetCadesSignaturePolicy());

				// Set signer's certificate
				cadesSigner.SetSigningCertificate(PKCertificate.Decode(Convert.FromBase64String(model.CertContent)));

				// Set the signature computed on the client-side, along with the "to-sign-bytes" recovered from the database
				cadesSigner.SetPrecomputedSignature(Convert.FromBase64String(model.Signature), Convert.FromBase64String(model.ToSignBytes));

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				cadesSigner.ComputeSignature();

				// Get the signature as an array of bytes
				signatureContent = cadesSigner.GetSignature();

			} catch (ValidationException ex) {
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			var extension = ".p7s";
			var filename = Storage.StoreFile(signatureContent, extension).Replace('.', '_');

			return RedirectToAction("SignatureInfo", new SignatureInfoModel() {
				File = filename
			});
		}

		// GET: CadesSignature/SignatureInfo
		[HttpGet]
		public ActionResult SignatureInfo(SignatureInfoModel model) {
			return View(model);
		}
	}
}