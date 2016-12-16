using Lacuna.Pki;
using Lacuna.Pki.Pades;
using MVC.Classes;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVC.Controllers {
    public class PadesSignatureController : Controller {

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

		// GET: PadesSignature
		public ActionResult Index() {
			return View();
		}

		/**
		* POST: PadesSignature
		* 
		* This action is called once the user's certificate encoding has been read, and contains the 
		* logic to prepare the byte array that needs to be actuallu signed with the user's private key
		* (the "to-sign-byte").
		*/
		[HttpPost]
		public ActionResult Index(SignatureStartModel model) {
			byte[] toSignBytes, transferData;
			SignatureAlgorithm signatureAlg;

			try {

				// Decode the user's certificate
				var cert = PKCertificate.Decode(model.CertContent);

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
				var visual = new PadesVisualRepresentation() {
					SignerName = cert.SubjectDisplayName,  // used to compose the message "Signed by XXX"
					SigningTime = true,                    // whether to include or not the signing time on the visual representation
					ImageBytes = Storage.GetPdfStampContent() // background image of the visual representation
				};
				visual.SetPosition(PadesVisualPosition.Footnote); // set the position of the visual representation
				padesSigner.SetVisualRepresentation(visual);

				// Generate the "to-sign-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy, as well as the "transfer data",
				// a byte-array that will be needed on the next step.
				toSignBytes = padesSigner.GetToSignBytes(out signatureAlg, out transferData);
			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate
				// encoding cannot be read or if the certificate is expired.
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// On the next step (Complete action), we'll need once again some information:
			// - The content of the selected certificate used to validate the signature in complete action.
			// - The thumbprint of the selected certificate
			// - The "to-sign-bytes"
			// - The "to-sign-hash" (digest of the "to-sign-bytes")
			// - The OID of the digest algorithm to be used during the signature operation
			// We'll store these values on TempData, which is a dictionary shared between actions.
			TempData["SignatureCompleteModel"] = new SignatureCompleteModel() {
				CertContent = model.CertContent,
				CertThumb = model.CertThumb,
				TransferData = transferData,
				ToSignHash = signatureAlg.DigestAlgorithm.ComputeHash(toSignBytes),
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid
			};

			return RedirectToAction("Complete");
		}

		// GET: CadesSignature/Complete
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
		* POST: CadesSignature/Complete
		* 
		* This action is called once the "to-sign-bytes" are signed using the user's certificate. After signature,
		* it'll be redirect to SignatureInfo action to show the signature file.
		*/
		[HttpPost]
		public ActionResult Complete(SignatureCompleteModel model) {
			byte[] signatureContent;

			try {

				var padesSigner = new PadesSigner();

				// Set the signature policy, exactly like in the Start method
				padesSigner.SetPolicy(getSignaturePolicy());

				// Set the signature computed on the client-side, along with the "transfer data" recovered from the database
				padesSigner.SetPreComputedSignature(model.Signature, model.TransferData);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				padesSigner.ComputeSignature();

				// Get the signed PDF as an array of bytes
				signatureContent = padesSigner.GetPadesSignature();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// Store the signature file on the folder "App_Data/" and redirects to the SignatureInfo action with the filename.
			// With this filename, it can show a link to download the signature file.
			var file = Storage.StoreFile(signatureContent, ".pdf");

			// On the next step (Complete action), we'll need once again some information:
			// - The content of the selected certificate used to validate the signature in complete action.
			// - The filename to be available to download in next action.
			// We'll store these values on TempData, which is a dictionary shared between actions.
			TempData["SignatureInfoModel"] = new SignatureInfoModel() {
				File = file,
				UserCert = PKCertificate.Decode(model.CertContent)
			};

			return RedirectToAction("SignatureInfo");
		}

		// GET: PadesSignature/SignatureInfo
		[HttpGet]
		public ActionResult SignatureInfo() {

			// Recovery data from Conplete() action, if returns null, it'll be redirected to Index 
			// action again.
			var model = TempData["SignatureInfoModel"] as SignatureInfoModel;
			if (model == null) {
				return RedirectToAction("Index");
			}

			return View(model);
		}
	}
}