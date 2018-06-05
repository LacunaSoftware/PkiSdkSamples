using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleMVC.Models {
	public class SignatureStartModel {
		public byte[] CertThumb { get; set; }
		public byte[] CertContent { get; set; }
	}

	public class SignatureCompleteResponse {
		public string Filename { get; set; }
		public CertificateModel Certificate { get; set; }
	}
}