using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Models {
	public class AuthenticationInfoModel {
		public PKCertificate UserCert { get; set; }
	}
}