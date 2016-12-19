using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models {
	public class SignatureCompleteResponse {
		public string Filename { get; set; }
		public CertificateModel Certificate { get; set; }
	}
}