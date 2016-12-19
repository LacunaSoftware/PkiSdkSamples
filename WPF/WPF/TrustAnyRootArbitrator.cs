using Lacuna.Pki;
using Lacuna.Pki.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWpfApp {
	class TrustAnyRootArbitrator : ITrustArbitrator {
		public ICertificateStore GetCertificateStore() {
			return null;
		}

		public bool IsRootTrusted(PKCertificate root, DateTimeOffset? dateReference, out ValidationResults vr) {
			vr = null;
			return true;
		}
	}
}
