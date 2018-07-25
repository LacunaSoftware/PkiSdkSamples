using Lacuna.Pki;
using Lacuna.Pki.Pades;

namespace WebForms.Classes {
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