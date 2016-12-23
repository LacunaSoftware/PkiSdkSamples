using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWpfApp {

	class CertificateItem {

		public PKCertificateWithKey CertificateWithKey { get; private set; }

		public CertificateItem(PKCertificateWithKey cert) {
			this.CertificateWithKey = cert;
		}

		public override string ToString() {
			return string.Format("{0} (expires on {1:d}, issued by {2})", CertificateWithKey.Certificate.SubjectName.CommonName, CertificateWithKey.Certificate.ValidityEnd, CertificateWithKey.Certificate.IssuerName.CommonName);
		}
	}
}
