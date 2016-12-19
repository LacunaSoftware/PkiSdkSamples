using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models {
	public class SignatureStartResponse {
		public byte[] ToSignBytes { get; set; }
		public string DigestAlgorithmOid { get; set; }
	}
}