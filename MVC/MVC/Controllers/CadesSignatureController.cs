﻿using Lacuna.Pki;
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

		// GET: CadesSignature
		[HttpGet]
		public ActionResult Index() {

			return View();
		}

		/**
		 * POST: CadesSignature
		 * 
		 * This action is called once the user's certificate encoding has been read, and contains the
		 * logic to prepare the byte array that needs to be actually signed with the user's private key
		 * (the "to-sign-bytes).
		 */ 
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
				cadesSigner.SetSigningCertificate(PKCertificate.Decode(model.CertContent));

				// Set the signature policy
				cadesSigner.SetPolicy(getSignaturePolicy());

				// Generate the "to-sign-bytes". This method also yields the signature algorithm that must
				// be used on the client-side, based on the signature policy.
				toSignBytes = cadesSigner.GenerateToSignBytes(out signatureAlg);
				

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
				ToSignBytes = toSignBytes,
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

				var cadesSigner = new CadesSigner();

				// Set the document to be signed and the policy, exactly like in the Start method
				cadesSigner.SetDataToSign(Storage.GetSampleDocContent());
				cadesSigner.SetPolicy(getSignaturePolicy());

				// Set signer's certificate
				cadesSigner.SetSigningCertificate(PKCertificate.Decode(model.CertContent));

				// Set the signature computed on the client-side, along with the "to-sign-bytes" recovered from the database
				cadesSigner.SetPrecomputedSignature(model.Signature, model.ToSignBytes);

				// Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the resulting signature
				cadesSigner.ComputeSignature();

				// Get the signature as an array of bytes
				signatureContent = cadesSigner.GetSignature();

			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				ModelState.AddModelError("", ex.ValidationResults.ToString());
				return View();
			}

			// Store the signature file on the folder "App_Data/" and redirects to the SignatureInfo action with the filename.
			// With this filename, it can show a link to download the signature file.
			var file = Storage.StoreFile(signatureContent, ".p7s");
			return RedirectToAction("SignatureInfo", new SignatureInfoModel() {
				File = file
			});
		}

		// GET: CadesSignature/SignatureInfo
		[HttpGet]
		public ActionResult SignatureInfo(SignatureInfoModel model) {
			return View(model);
		}
	}
}