using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models {
	public class AuthenticationPostRequest {
		public byte[] Certificate { get; set; }
		public byte[] Nonce { get; set; }
		public byte[] Signature { get; set; }
	}
}