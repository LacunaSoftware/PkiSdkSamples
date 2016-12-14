﻿using Lacuna.Pki;
using MVC.Classes;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVC.Controllers {

	public class AuthenticationController : Controller {

		/**
		 * GET: Authentication
		 * 
		 * This action sets a NonceStore needed for authentication. And if it is rendered by some error
		 * that accured, it will show the error message to the user and generate another instance of a 
		 * NonceStore, letting the user to sign again.
		 */
		[HttpGet]
		public ActionResult Index() {

			// The PKCertificateAuthentication class requires an implementation of INonceStore.
			// We'll use the FileSystemNonceStore class.
			var nonceStore = Util.GetNonceStore();

			// Instantiate the PKCertificateAuthentication class passing our EntityFrameworkNonceStore
			var certAuth = new PKCertificateAuthentication(nonceStore);

			// Call the Start() method, which is the first of the two server-side steps. This yields the nonce,
			// a 16-byte-array which we'll send to the view.
			var nonce = certAuth.Start();

			// Notice that this previous will be executed even if an error has occured and it was redirected 
			// to this action.
			var model = new AuthenticationModel() {
				Nonce = nonce,
				DigestAlgorithm = PKCertificateAuthentication.DigestAlgorithm.Oid
			};

			// If this action is called because occurred an error. This error's message will be
			// shown
			var vr = TempData["ValidationResults"] as ValidationResults;
			if (vr != null && !vr.IsValid) {
				ModelState.AddModelError("", vr.ToString());
			}
			return View(model);
		}

		/**
		 * POST: Authentication
		 * 
		 * This action is called once the user clicks the "Sign In" button. It uses the PKCertificateAuthentication
		 * class to generate and store a cryptographic nonce, which will then be sent to the view for signature using
		 * the user's certificate.
		 */
		[HttpPost]
		public ActionResult Index(AuthenticationModel model) {

			// As before, we instantiate a FileSystemNonceStore class and use that to 
			// instantiate a PKCertificateAuthentication
			var nonceStore = Util.GetNonceStore();
			var certAuth = new PKCertificateAuthentication(nonceStore);

			// Call the Complete() method, which is the last of the two server-side steps. It receives:
			// - The nonce which was signed using the user's certificate
			// - The user's certificate encoding
			// - The nonce signature
			// - A TrustArbitrator to be used to determine trust in the certificate (for more information see http://pki.lacunasoftware.com/Help/html/e7724d78-9835-4f06-b58c-939b721f6e7b.htm)
			// The call yields:
			// - A ValidationResults which denotes whether the authentication was successful or not
			// - The user's decoded certificate
			PKCertificate certificate;
			var vr = certAuth.Complete(model.Nonce, model.Certificate, model.Signature, Util.GetTrustArbitrator(), out certificate);

			// NOTE: By changing the TrustArbitrator above, you can accept only certificates from a certain PKI,
			// for instance, ICP-Brasil (TrustArbitrators.PkiBrazil). For more information, see
			// http://pki.lacunasoftware.com/Help/html/e7724d78-9835-4f06-b58c-939b721f6e7b.htm
			//
			// The value above (TrustArbitrators.Windows) specifies that the root certification authorities in the
			// Windows certificate store are to be used as trust arbitrators.

			// Check the authentication result
			if (!vr.IsValid) {
				// The authentication failed, redirect and inform in index view.
				TempData["ValidationResults"] = vr;
				return RedirectToAction("Index");
			}

			// We redirect to AuthenticationInfo action, that renders the certificate infomations
			// in its viwe
			return View("AuthenticationInfo", new AuthenticationInfoModel() {
				UserCert = PKCertificate.Decode(model.Certificate)
			});
		}

	}
}