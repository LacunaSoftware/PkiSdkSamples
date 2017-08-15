using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWpfApp {

	class SignerItem {

		public string Description { get; private set; }
		public ValidationResults ValidationResults { get; private set; }

		public SignerItem(string description, ValidationResults vr) {
			this.Description = description;
			this.ValidationResults = vr;

		}

		public override string ToString() {
			return Description;
		}
	}
}
