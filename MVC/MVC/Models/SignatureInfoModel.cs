﻿using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Models {
	public class SignatureInfoModel {
		public string Filename { get; set; }
		public PKCertificate UserCert { get; set; }
	}
}