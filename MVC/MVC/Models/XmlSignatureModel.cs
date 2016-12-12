using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Models {
	public class XmlSignatureModel {
		public string TransferData { get; set; }
		public string Certificate { get; set; }
		public string CertThumb { get; set; }
		public string ToSignHash { get; set; }
		public string Signature { get; set; }
		public string DigestAlgorithmOid { get; set; }
	}
}