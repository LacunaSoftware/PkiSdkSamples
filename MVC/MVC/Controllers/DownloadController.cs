using MVC.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace MVC.Controllers {
	/**
	 * This controller's purpose is to download the sample file that is signed during the
	 * signature examples or download a upload file for signature or download a previously performed
	 * signature. The actual work for performing signatures is done in the controllers CadesSignatureController
	 * and PadesSignatureController.
	 */
	public class DownloadController : Controller {
		// GET Download/File/{id}
		[HttpGet]
		public ActionResult File(string id) {
			byte[] content;
			string extension;
			if (!Storage.TryGetFile(id, out content, out extension)) {
				return HttpNotFound();
			}
			var filename = id + extension;
			return File(content, MimeMapping.GetMimeMapping(filename), filename);
		}

		// GET Download/Sample
		[HttpGet]
		public ActionResult Sample() {
			var fileContent = Storage.GetSampleDocContent();
			return File(fileContent, "application/pdf", "Sample.pdf");
		}

		// GET Download/SampleNFe
		[HttpGet]
		public ActionResult SampleNFe() {
			var fileContent = Storage.GetSampleNFeContent();
			return File(fileContent, "text/xml", "SampleNFe.xml");
		}

		// GET Download/SampleCodEnvelope
		[HttpGet]
		public ActionResult SampleCodEnvelope() {
			var fileContent = Storage.GetSampleCodEnvelope();
			return File(fileContent, "text/xml", "SampleCodEnvelope.xml");
		}
	}
}