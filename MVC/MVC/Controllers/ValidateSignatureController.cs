using Lacuna.Pki.Xml;
using MVC.Classes;
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
			if (!Storage.TryGetFile(id, out content, out extension)) {
				return HttpNotFound();
			}

			var xmlSignatureLocator = new XmlSignatureLocator(content);
			var model = xmlSignatureLocator.GetSignatures();
			return View(model);
		}
	}

}
