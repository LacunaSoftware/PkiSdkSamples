using Lacuna.Pki;
using Lacuna.Pki.Pades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC.Classes {
	public class PadesPolicyMapperWrapper : IPadesPolicyMapper {

		private IPadesPolicyMapper policyMapper;
		private ITrustArbitrator trustArbitrator;

		public PadesPolicyMapperWrapper(IPadesPolicyMapper policyMapper, ITrustArbitrator trustArbitrator) {
			this.policyMapper = policyMapper;
			this.trustArbitrator = trustArbitrator;
		}

		public PadesPolicySpec GetPolicy(PKCertificate certificate) {
			var policy = policyMapper.GetPolicy(certificate);
			policy.ClearTrustArbitrators();
			policy.AddTrustArbitrator(trustArbitrator);
			return policy;
		}
	}
}