using Lacuna.Pki;
using MVC.Classes;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVC.Controllers {

	public class AuthenticationController : Controller {

		[HttpGet]
		public ActionResult Index() {
			var nonceStore = Util.GetNonceStore();
			var certAuth = new PKCertificateAuthentication(nonceStore);
			var nonce = certAuth.Start();
			var model = new AuthenticationModel() {
				Nonce = nonce,
				DigestAlgorithm = PKCertificateAuthentication.DigestAlgorithm.Oid
			};
			var vr = TempData["ValidationResults"] as ValidationResults;
			if (vr != null && !vr.IsValid) {
				ModelState.AddModelError("", vr.ToString());
			}
			return View(model);
		}

		[HttpPost]
		public ActionResult Index(AuthenticationModel model) {

			var nonceStore = Util.GetNonceStore();
			var certAuth = new PKCertificateAuthentication(nonceStore);

			PKCertificate certificate;
			var vr = certAuth.Complete(model.Nonce, model.Certificate, model.Signature, Util.GetTrustArbitrator(), out certificate);
			if (!vr.IsValid) {
				TempData["ValidationResults"] = vr;
				return RedirectToAction("Index");
			}

			return View("AuthenticationInfo", new AuthenticationInfoModel() {
				UserCert = PKCertificate.Decode(model.Certificate)
			});
		}

	}
}