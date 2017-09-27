using Lacuna.Pki.Xml;
using MVC.Classes;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVC.Controllers {

	public class ValidateSignatureController : Controller {

		// GET ValidateSignature/Xml
		public ActionResult Xml(string id) {

			byte[] content;
			string extension;
			if (!StorageMock.TryGetFile(id, out content, out extension)) {
				return HttpNotFound();
			}

			var xmlSignatureLocator = new XmlSignatureLocator(content);
            var signatures = xmlSignatureLocator.GetSignatures();

            // Validate signatures
            var validationPolicy = XmlPolicySpec.GetXmlDSigBasic(Util.GetTrustArbitrator());
            var model = new List<XmlSignatureModel>();
            foreach (var signature in signatures) {
                model.Add(new XmlSignatureModel() {
                    Signature = signature,
                    ValidationResults = signature.Validate(validationPolicy)
                });
            }

            // Render validation page
			return View(model);
		}
	}

}
