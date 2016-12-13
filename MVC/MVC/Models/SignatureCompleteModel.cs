using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Models {
	public class SignatureCompleteModel {
		public string CertThumb { get; set; }
		public string ToSignHash { get; set; }
		public string TransferData { get; set; }
		public string DigestAlgorithmOid { get; set; }
		public string Signature { get; set; }
	}
}