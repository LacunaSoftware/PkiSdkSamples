﻿using MVC.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace MVC.Controllers
{
	/**
	 * This controller's purpose is to download the sample file that is signed during the
	 * signature examples or download a upload file for signature or download a previously performed
	 * signature. The actual work for performing signatures is done in the controllers CadesSignatureController
	 * and PadesSignatureController.
	 */
	public class DownloadController : Controller
    {
		// GET Download/File/{id}
		[HttpGet]
		public ActionResult File(string id) {
			if (string.IsNullOrEmpty(id)) {
				return HttpNotFound();
			}
			var filename = id.Replace("_", "."); // Note: we're passing the filename argument with "." as "_" because of limitations of ASP.NET MVC
			var path = HostingEnvironment.MapPath(string.Format("~/App_Data/{0}", filename));
			if (!System.IO.File.Exists(path)) {
				return HttpNotFound();
			}
			var content = System.IO.File.ReadAllBytes(path);
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
	}
}