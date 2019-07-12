using Lacuna.Pki.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Classes;
using WebApi.Models;

namespace WebApi.Controllers {
	public class OpenXmlSignatureController : ApiController {


		[HttpPost]
		public IHttpActionResult Post(OpenXmlSignatureRequest request) {

			// This sample requires the FileId field is valid and corresponds to an existing file.
			if (string.IsNullOrEmpty(request.FileId)) {
				return BadRequest();
			}

			// Verifies the existence of the FileId and read its content.
			byte[] content;
			if (!Storage.TryGetFile(request.FileId, out content)) {
				return NotFound();
			}

			// Get an instance of the XmlSignatureLocator class, which is responsible to open the 
			// signed XML.
			var xmlSignatureLocator = new XmlSignatureLocator(content);
			var signatures = xmlSignatureLocator.GetSignatures();
			var validationPolicy = XmlPolicySpec.GetXmlDSigBasic(Util.GetTrustArbitrator());
			var vrs = signatures.ToDictionary(s => s, s => s.Validate(validationPolicy));
			return Ok(new OpenXmlSignatureResponse(signatures, vrs));
		}

	}
}
