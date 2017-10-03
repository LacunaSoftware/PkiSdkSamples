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

			if (request.FileContent == null || request.FileContent.Length < 1) {
				return NotFound();
			}

			var xmlSignatureLocator = new XmlSignatureLocator(request.FileContent);
			var signatures = xmlSignatureLocator.GetSignatures();
			var validationPolicy = XmlPolicySpec.GetXmlDSigBasic(Util.GetTrustArbitrator());
			var vrs = signatures.ToDictionary(s => s, s => s.Validate(validationPolicy));
			return Ok(new OpenXmlSignatureResponse(signatures, vrs));
		}

	}
}
